using System.Net;
using AwesomeAssertions;
using FakeItEasy;
using GhInfo.Cli;
using GhInfo.GitHub;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GhInfo.Tests.Cli;

public sealed class UserInfoCommandTests
{
    [Fact]
    public async Task ExecuteAsync_OnSuccess_RendersUserAndReturnsZero()
    {
        // Arrange
        var user = MakeUser();
        var service = A.Fake<IGhInfoService>();
        A.CallTo(() => service.GetUserAsync("octocat", true, A<CancellationToken>._))
            .Returns(new GhInfoResult(user, FromCache: false));
        var renderer = A.Fake<IUserTableRenderer>();
        ICommand command = CreateCommand(service, renderer);

        // Act
        var exitCode = await command.ExecuteAsync(
            CreateContext(),
            new UserInfoCommand.Settings { Username = "octocat", NoCache = false },
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
        A.CallTo(() => renderer.Render(user, false)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheHit_RendersWithFromCacheTrue()
    {
        // Arrange
        var user = MakeUser();
        var service = A.Fake<IGhInfoService>();
        A.CallTo(() => service.GetUserAsync(A<string>._, A<bool>._, A<CancellationToken>._))
            .Returns(new GhInfoResult(user, FromCache: true));
        var renderer = A.Fake<IUserTableRenderer>();
        ICommand command = CreateCommand(service, renderer);

        // Act
        var exitCode = await command.ExecuteAsync(
            CreateContext(),
            new UserInfoCommand.Settings { Username = "octocat", NoCache = false },
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
        A.CallTo(() => renderer.Render(user, true)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoCacheFlag_PassesUseCacheFalse()
    {
        // Arrange
        var service = A.Fake<IGhInfoService>();
        A.CallTo(() => service.GetUserAsync(A<string>._, A<bool>._, A<CancellationToken>._))
            .Returns(new GhInfoResult(MakeUser(), FromCache: false));
        ICommand command = CreateCommand(service);

        // Act
        _ = await command.ExecuteAsync(
            CreateContext(),
            new UserInfoCommand.Settings { Username = "octocat", NoCache = true },
            CancellationToken.None);

        // Assert
        A.CallTo(() => service.GetUserAsync("octocat", false, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExecuteAsync_OnUserNotFound_ReturnsExit1AndDoesNotRender()
    {
        // Arrange
        var service = A.Fake<IGhInfoService>();
        A.CallTo(() => service.GetUserAsync(A<string>._, A<bool>._, A<CancellationToken>._))
            .ThrowsAsync(new GitHubUserNotFoundException("ghost"));
        var renderer = A.Fake<IUserTableRenderer>();
        ICommand command = CreateCommand(service, renderer);

        // Act
        var exitCode = await command.ExecuteAsync(
            CreateContext(),
            new UserInfoCommand.Settings { Username = "ghost", NoCache = false },
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(1);
        A.CallTo(renderer).MustNotHaveHappened();
    }

    [Fact]
    public async Task ExecuteAsync_OnGitHubApiException_ReturnsExit2()
    {
        // Arrange
        var service = A.Fake<IGhInfoService>();
        A.CallTo(() => service.GetUserAsync(A<string>._, A<bool>._, A<CancellationToken>._))
            .ThrowsAsync(new GitHubApiException(HttpStatusCode.InternalServerError, "boom"));
        ICommand command = CreateCommand(service);

        // Act
        var exitCode = await command.ExecuteAsync(
            CreateContext(),
            new UserInfoCommand.Settings { Username = "octocat", NoCache = false },
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_OnHttpRequestException_ReturnsExit3()
    {
        // Arrange
        var service = A.Fake<IGhInfoService>();
        A.CallTo(() => service.GetUserAsync(A<string>._, A<bool>._, A<CancellationToken>._))
            .ThrowsAsync(new HttpRequestException("dns"));
        ICommand command = CreateCommand(service);

        // Act
        var exitCode = await command.ExecuteAsync(
            CreateContext(),
            new UserInfoCommand.Settings { Username = "octocat", NoCache = false },
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellationToken()
    {
        // Arrange
        var service = A.Fake<IGhInfoService>();
        A.CallTo(() => service.GetUserAsync(A<string>._, A<bool>._, A<CancellationToken>._))
            .Returns(new GhInfoResult(MakeUser(), FromCache: false));
        ICommand command = CreateCommand(service);
        using var cts = new CancellationTokenSource();

        // Act
        _ = await command.ExecuteAsync(
            CreateContext(),
            new UserInfoCommand.Settings { Username = "octocat", NoCache = false },
            cts.Token);

        // Assert
        A.CallTo(() => service.GetUserAsync("octocat", true, cts.Token))
            .MustHaveHappenedOnceExactly();
    }

    private static UserInfoCommand CreateCommand(
        IGhInfoService service,
        IUserTableRenderer? renderer = null,
        IAnsiConsole? console = null)
    {
        return new UserInfoCommand(
            service,
            renderer ?? A.Fake<IUserTableRenderer>(),
            console ?? A.Fake<IAnsiConsole>(),
            NullLogger<UserInfoCommand>.Instance);
    }

    private static CommandContext CreateContext()
    {
        return new CommandContext(
            Array.Empty<string>(),
            A.Fake<IRemainingArguments>(),
            "user-info",
            data: null);
    }

    private static GitHubUser MakeUser()
    {
        return new GitHubUser
        {
            Login = "octocat",
            Name = "The Octocat",
            Bio = "tentacles",
            PublicRepos = 8,
            Followers = 100,
            CreatedAt = new DateTimeOffset(2011, 1, 25, 0, 0, 0, TimeSpan.Zero)
        };
    }
}
