using AwesomeAssertions;
using FakeItEasy;
using GhInfo;
using GhInfo.Caching;
using GhInfo.Cli;
using GhInfo.GitHub;
using GhInfo.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace GhInfo.Tests.Cli;

public sealed class UserInfoCommandTests
{
    [Fact]
    public async Task Execute_WithExistingUser_PrintsTableAndReturnsZero()
    {
        // Arrange
        var fakeClient = new FakeGitHubUsersClient();
        fakeClient.AddUser(CreateUser("octocat"));
        var (app, console) = CreateApp(fakeClient);

        // Act
        var exitCode = await app.RunAsync(new[] { "octocat", "--no-cache" });

        // Assert
        exitCode.Should().Be(0);
        console.Output.Should().Contain("octocat").And.Contain("The Octocat");
    }

    [Fact]
    public async Task Execute_WithUnknownUser_ReturnsExitCodeOne()
    {
        // Arrange
        var fakeClient = new FakeGitHubUsersClient();
        var (app, console) = CreateApp(fakeClient);

        // Act
        var exitCode = await app.RunAsync(new[] { "ghost", "--no-cache" });

        // Assert
        exitCode.Should().Be(1);
        console.Output.Should().Contain("No GitHub user found");
    }

    [Fact]
    public async Task Execute_WhenApiThrows_ReturnsExitCodeTwo()
    {
        // Arrange
        var fakeClient = new FakeGitHubUsersClient
        {
            ExceptionToThrow = new GitHubApiException(System.Net.HttpStatusCode.BadGateway, responseBody: null, "upstream"),
        };
        var (app, console) = CreateApp(fakeClient);

        // Act
        var exitCode = await app.RunAsync(new[] { "octocat", "--no-cache" });

        // Assert
        exitCode.Should().Be(2);
        console.Output.Should().Contain("GitHub API error");
    }

    private static (CommandApp<UserInfoCommand> App, TestConsole Console) CreateApp(IGitHubUsersClient gitHubUsersClient)
    {
        var console = new TestConsole();

        var services = new ServiceCollection();
        services.AddSingleton<IAnsiConsole>(console);
        services.AddSingleton(gitHubUsersClient);
        services.AddSingleton(A.Fake<IUserCacheService>());
        services.AddSingleton<IGhInfoService, GhInfoService>();
        services.AddSingleton<IUserTableRenderer, UserTableRenderer>();
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddTransient<UserInfoCommand>();

        var registrar = new TypeRegistrar(services.BuildServiceProvider());
        var app = new CommandApp<UserInfoCommand>(registrar);
        app.Configure(c => c.PropagateExceptions());

        return (app, console);
    }

    private static GitHubUser CreateUser(string login)
    {
        return new GitHubUser(
            login,
            Name: "The Octocat",
            Bio: "GitHub mascot",
            PublicRepos: 8,
            Followers: 1234,
            CreatedAt: DateTimeOffset.Parse("2008-01-14T04:33:35Z"));
    }
}
