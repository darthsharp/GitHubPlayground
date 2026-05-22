using AwesomeAssertions;
using FakeItEasy;
using GhInfo.Core;
using GhInfo.Core.Caching;
using GhInfo.Core.GitHub;
using GhInfo.Core.Models;
using GhInfo.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace GhInfo.Core.Tests;

public class GitHubUserServiceTests
{
    private static GitHubUser CreateUser(string login = "octocat")
    {
        return new GitHubUser
        {
            Login = login,
            Name = "The Octocat",
            PublicRepos = 8,
            Followers = 100,
            CreatedAt = new DateTimeOffset(2011, 1, 25, 0, 0, 0, TimeSpan.Zero),
        };
    }

    private static GitHubUserService CreateSut(IGitHubUsersClient client, IUserCacheService cache)
    {
        return new GitHubUserService(client, cache, NullLogger<GitHubUserService>.Instance);
    }

    [Fact]
    public async Task GetUserAsync_WhenCacheHasFreshEntry_ReturnsCachedAndSkipsApi()
    {
        // Arrange
        var client = new FakeGitHubUsersClient();
        var cache = A.Fake<IUserCacheService>();
        var cached = CreateUser();
        A.CallTo(() => cache.GetAsync("octocat", A<CancellationToken>._)).Returns(cached);
        var sut = CreateSut(client, cache);

        // Act
        var result = await sut.GetUserAsync("octocat");

        // Assert
        result.FromCache.Should().BeTrue();
        result.User.Should().BeSameAs(cached);
        client.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserAsync_WhenCacheMisses_FetchesFromApiAndCaches()
    {
        // Arrange
        var client = new FakeGitHubUsersClient();
        client.AddUser(CreateUser());
        var cache = A.Fake<IUserCacheService>();
        A.CallTo(() => cache.GetAsync("octocat", A<CancellationToken>._)).Returns((GitHubUser?)null);
        var sut = CreateSut(client, cache);

        // Act
        var result = await sut.GetUserAsync("octocat");

        // Assert
        result.FromCache.Should().BeFalse();
        result.User.Login.Should().Be("octocat");
        client.CallCount.Should().Be(1);
        A.CallTo(() => cache.SetAsync(result.User, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserAsync_WhenCacheDisabled_BypassesCacheReadButStillCaches()
    {
        // Arrange
        var client = new FakeGitHubUsersClient();
        client.AddUser(CreateUser());
        var cache = A.Fake<IUserCacheService>();
        var sut = CreateSut(client, cache);

        // Act
        var result = await sut.GetUserAsync("octocat", useCache: false);

        // Assert
        result.FromCache.Should().BeFalse();
        client.CallCount.Should().Be(1);
        A.CallTo(() => cache.GetAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => cache.SetAsync(result.User, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserAsync_WhenUserNotFound_PropagatesException()
    {
        // Arrange
        var client = new FakeGitHubUsersClient
        {
            ExceptionToThrow = new GitHubUserNotFoundException("ghost"),
        };
        var cache = A.Fake<IUserCacheService>();
        A.CallTo(() => cache.GetAsync("ghost", A<CancellationToken>._)).Returns((GitHubUser?)null);
        var sut = CreateSut(client, cache);

        // Act
        var act = async () => await sut.GetUserAsync("ghost");

        // Assert
        await act.Should().ThrowAsync<GitHubUserNotFoundException>();
        A.CallTo(() => cache.SetAsync(A<GitHubUser>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserAsync_WithInvalidLogin_Throws(string? login)
    {
        // Arrange
        var sut = CreateSut(new FakeGitHubUsersClient(), A.Fake<IUserCacheService>());

        // Act
        var act = async () => await sut.GetUserAsync(login!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
