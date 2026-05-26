using System.Net;
using AwesomeAssertions;
using GhInfo.GitHub;
using GhInfo.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace GhInfo.Tests.GitHub;

public sealed class GitHubUsersClientTests
{
    private const string OctocatJson = """
        {
          "login": "octocat",
          "name": "The Octocat",
          "bio": "GitHub mascot",
          "public_repos": 8,
          "followers": 1234,
          "created_at": "2008-01-14T04:33:35Z"
        }
        """;

    [Fact]
    public async Task GetUserAsync_OnSuccess_ReturnsParsedUser()
    {
        // Arrange
        var handler = StubHttpMessageHandler.ReturnsJson(HttpStatusCode.OK, OctocatJson);
        var sut = CreateSut(handler);

        // Act
        var user = await sut.GetUserAsync("octocat");

        // Assert
        user.Should().NotBeNull();
        user!.Login.Should().Be("octocat");
        user.Name.Should().Be("The Octocat");
        user.PublicRepos.Should().Be(8);
        user.Followers.Should().Be(1234);
        user.CreatedAt.Should().Be(DateTimeOffset.Parse("2008-01-14T04:33:35Z"));
        handler.Requests.Should().ContainSingle()
            .Which.RequestUri!.ToString().Should().Be("https://api.github.com/users/octocat");
    }

    [Fact]
    public async Task GetUserAsync_On404_ReturnsNull()
    {
        // Arrange
        var handler = StubHttpMessageHandler.ReturnsStatus(HttpStatusCode.NotFound);
        var sut = CreateSut(handler);

        // Act
        var user = await sut.GetUserAsync("ghost");

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_OnServerError_ThrowsGitHubApiException()
    {
        // Arrange
        var handler = StubHttpMessageHandler.ReturnsStatus(HttpStatusCode.InternalServerError, "boom");
        var sut = CreateSut(handler);

        // Act
        Func<Task> act = () => sut.GetUserAsync("octocat");

        // Assert
        var ex = await act.Should().ThrowAsync<GitHubApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        ex.Which.ResponseBody.Should().Be("boom");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserAsync_WithBlankLogin_Throws(string login)
    {
        // Arrange
        var handler = StubHttpMessageHandler.ReturnsJson(HttpStatusCode.OK, OctocatJson);
        var sut = CreateSut(handler);

        // Act
        Func<Task> act = () => sut.GetUserAsync(login);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static GitHubUsersClient CreateSut(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/"),
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("gh-info-tests");

        return new GitHubUsersClient(httpClient, NullLogger<GitHubUsersClient>.Instance);
    }
}
