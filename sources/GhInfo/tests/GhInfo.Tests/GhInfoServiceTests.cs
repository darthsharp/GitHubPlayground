using AwesomeAssertions;
using FakeItEasy;
using GhInfo.Caching;
using GhInfo.GitHub;
using GhInfo.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace GhInfo.Tests;

public sealed class GhInfoServiceTests
{
    [Fact]
    public async Task GetUserAsync_WithCacheHit_ReturnsCachedAndDoesNotCallApi()
    {
        // Arrange
        var fakeClient = new FakeGitHubUsersClient();
        var cache = A.Fake<IUserCacheService>();
        var cached = MakeUser("octocat");
        A.CallTo(() => cache.TryGetAsync("octocat", A<CancellationToken>._))
            .Returns(Task.FromResult<GitHubUser?>(cached));
        var sut = new GhInfoService(fakeClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var result = await sut.GetUserAsync("octocat", useCache: true);

        // Assert
        result.User.Should().BeSameAs(cached);
        result.FromCache.Should().BeTrue();
        fakeClient.CallCount.Should().Be(0);
        A.CallTo(() => cache.SetAsync(A<GitHubUser>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetUserAsync_WithCacheMiss_FetchesFromApiAndCachesResult()
    {
        // Arrange
        var fakeClient = new FakeGitHubUsersClient();
        var apiUser = MakeUser("octocat");
        fakeClient.AddUser(apiUser);
        var cache = A.Fake<IUserCacheService>();
        A.CallTo(() => cache.TryGetAsync("octocat", A<CancellationToken>._))
            .Returns(Task.FromResult<GitHubUser?>(null));
        var sut = new GhInfoService(fakeClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var result = await sut.GetUserAsync("octocat", useCache: true);

        // Assert
        result.User.Should().BeSameAs(apiUser);
        result.FromCache.Should().BeFalse();
        fakeClient.CallCount.Should().Be(1);
        A.CallTo(() => cache.SetAsync(apiUser, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserAsync_WithNoCache_BypassesCacheLookupAndStillRefreshes()
    {
        // Arrange
        var fakeClient = new FakeGitHubUsersClient();
        var apiUser = MakeUser("octocat");
        fakeClient.AddUser(apiUser);
        var cache = A.Fake<IUserCacheService>();
        var sut = new GhInfoService(fakeClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var result = await sut.GetUserAsync("octocat", useCache: false);

        // Assert
        result.FromCache.Should().BeFalse();
        fakeClient.CallCount.Should().Be(1);
        A.CallTo(() => cache.TryGetAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => cache.SetAsync(apiUser, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserAsync_WhenApiThrowsNotFound_Propagates()
    {
        // Arrange
        var fakeClient = new FakeGitHubUsersClient();
        var cache = A.Fake<IUserCacheService>();
        A.CallTo(() => cache.TryGetAsync(A<string>._, A<CancellationToken>._))
            .Returns(Task.FromResult<GitHubUser?>(null));
        var sut = new GhInfoService(fakeClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var act = async () => await sut.GetUserAsync("ghost", useCache: true);

        // Assert
        var ex = await act.Should().ThrowAsync<GitHubUserNotFoundException>();
        ex.Which.Login.Should().Be("ghost");
        A.CallTo(() => cache.SetAsync(A<GitHubUser>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetUserAsync_WithNullLogin_Throws()
    {
        // Arrange
        var sut = new GhInfoService(
            new FakeGitHubUsersClient(),
            A.Fake<IUserCacheService>(),
            NullLogger<GhInfoService>.Instance);

        // Act
        var act = async () => await sut.GetUserAsync(null!, useCache: true);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static GitHubUser MakeUser(string login)
    {
        return new GitHubUser
        {
            Login = login,
            Name = "Octo Cat",
            Bio = "bio",
            PublicRepos = 3,
            Followers = 4,
            CreatedAt = new DateTimeOffset(2011, 1, 25, 0, 0, 0, TimeSpan.Zero)
        };
    }
}
