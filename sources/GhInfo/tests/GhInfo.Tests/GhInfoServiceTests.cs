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
    public async Task GetUserAsync_WhenCacheHit_ReturnsCachedAndSkipsApi()
    {
        // Arrange
        var cache = A.Fake<IUserCacheService>();
        var cachedUser = CreateUser("octocat");
        A.CallTo(() => cache.GetAsync("octocat", A<CancellationToken>._)).Returns(cachedUser);

        var apiClient = new FakeGitHubUsersClient();
        var sut = new GhInfoService(apiClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var result = await sut.GetUserAsync("octocat", useCache: true);

        // Assert
        result.Should().BeSameAs(cachedUser);
        apiClient.GetUserCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserAsync_WhenCacheMiss_FetchesFromApiAndStores()
    {
        // Arrange
        var cache = A.Fake<IUserCacheService>();
        A.CallTo(() => cache.GetAsync(A<string>._, A<CancellationToken>._)).Returns((GitHubUser?)null);

        var apiClient = new FakeGitHubUsersClient();
        var apiUser = CreateUser("octocat");
        apiClient.AddUser(apiUser);

        var sut = new GhInfoService(apiClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var result = await sut.GetUserAsync("octocat", useCache: true);

        // Assert
        result.Should().BeSameAs(apiUser);
        apiClient.GetUserCallCount.Should().Be(1);
        A.CallTo(() => cache.SetAsync(apiUser, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserAsync_WithCacheBypass_NeitherReadsNorWritesCache()
    {
        // Arrange
        var cache = A.Fake<IUserCacheService>();
        var apiClient = new FakeGitHubUsersClient();
        apiClient.AddUser(CreateUser("octocat"));
        var sut = new GhInfoService(apiClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var result = await sut.GetUserAsync("octocat", useCache: false);

        // Assert
        result.Should().NotBeNull();
        A.CallTo(() => cache.GetAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => cache.SetAsync(A<GitHubUser>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetUserAsync_WhenApiReturnsNull_DoesNotCache()
    {
        // Arrange
        var cache = A.Fake<IUserCacheService>();
        A.CallTo(() => cache.GetAsync(A<string>._, A<CancellationToken>._)).Returns((GitHubUser?)null);

        var apiClient = new FakeGitHubUsersClient();
        var sut = new GhInfoService(apiClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        var result = await sut.GetUserAsync("ghost", useCache: true);

        // Assert
        result.Should().BeNull();
        A.CallTo(() => cache.SetAsync(A<GitHubUser>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetUserAsync_WhenApiThrows_PropagatesException()
    {
        // Arrange
        var cache = A.Fake<IUserCacheService>();
        A.CallTo(() => cache.GetAsync(A<string>._, A<CancellationToken>._)).Returns((GitHubUser?)null);

        var apiClient = new FakeGitHubUsersClient
        {
            ExceptionToThrow = new GitHubApiException(System.Net.HttpStatusCode.ServiceUnavailable, "down", "boom"),
        };
        var sut = new GhInfoService(apiClient, cache, NullLogger<GhInfoService>.Instance);

        // Act
        Func<Task> act = () => sut.GetUserAsync("octocat", useCache: true);

        // Assert
        await act.Should().ThrowAsync<GitHubApiException>();
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
