using System.Net;
using AwesomeAssertions;
using GhInfo.Core.GitHub;
using GhInfo.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace GhInfo.Core.Tests.GitHub;

public class GitHubUsersClientTests
{
    private const string OctocatJson =
        """
        {
            "login": "octocat",
            "name": "The Octocat",
            "bio": "A mascot",
            "public_repos": 8,
            "followers": 22730,
            "created_at": "2011-01-25T18:44:36Z"
        }
        """;

    private static GitHubUsersClient CreateSut(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };

        return new GitHubUsersClient(httpClient, NullLogger<GitHubUsersClient>.Instance);
    }

    [Fact]
    public async Task GetUserAsync_OnSuccess_MapsSnakeCaseJson()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, OctocatJson);
        var sut = CreateSut(handler);

        // Act
        var user = await sut.GetUserAsync("octocat");

        // Assert
        user.Login.Should().Be("octocat");
        user.Name.Should().Be("The Octocat");
        user.Bio.Should().Be("A mascot");
        user.PublicRepos.Should().Be(8);
        user.Followers.Should().Be(22730);
        user.CreatedAt.Should().Be(new DateTimeOffset(2011, 1, 25, 18, 44, 36, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetUserAsync_RequestsExpectedRelativePath()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, OctocatJson);
        var sut = CreateSut(handler);

        // Act
        await sut.GetUserAsync("octocat");

        // Assert
        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Be("https://api.github.com/users/octocat");
    }

    [Fact]
    public async Task GetUserAsync_OnNotFound_ThrowsUserNotFound()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(HttpStatusCode.NotFound, "{\"message\":\"Not Found\"}");
        var sut = CreateSut(handler);

        // Act
        var act = async () => await sut.GetUserAsync("ghost");

        // Assert
        var exception = await act.Should().ThrowAsync<GitHubUserNotFoundException>();
        exception.Which.Login.Should().Be("ghost");
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserAsync_OnForbiddenWithZeroRemaining_ThrowsRateLimit()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(HttpStatusCode.Forbidden, "rate limited")
        {
            ResponseHeaders = new Dictionary<string, string> { ["X-RateLimit-Remaining"] = "0" },
        };
        var sut = CreateSut(handler);

        // Act
        var act = async () => await sut.GetUserAsync("octocat");

        // Assert
        await act.Should().ThrowAsync<GitHubRateLimitException>();
    }

    [Fact]
    public async Task GetUserAsync_OnTooManyRequests_ThrowsRateLimit()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(HttpStatusCode.TooManyRequests, "slow down");
        var sut = CreateSut(handler);

        // Act
        var act = async () => await sut.GetUserAsync("octocat");

        // Assert
        await act.Should().ThrowAsync<GitHubRateLimitException>();
    }

    [Fact]
    public async Task GetUserAsync_OnServerError_ThrowsGitHubException()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "boom");
        var sut = CreateSut(handler);

        // Act
        var act = async () => await sut.GetUserAsync("octocat");

        // Assert
        var exception = await act.Should().ThrowAsync<GitHubException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserAsync_WithInvalidLogin_Throws(string? login)
    {
        // Arrange
        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.OK, OctocatJson));

        // Act
        var act = async () => await sut.GetUserAsync(login!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
