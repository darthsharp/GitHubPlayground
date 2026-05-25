using System.Net;
using System.Text;
using AwesomeAssertions;
using GhInfo.GitHub;
using GhInfo.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace GhInfo.Tests.GitHub;

public sealed class GitHubUsersClientTests
{
    private const string ValidUserJson = """
        {
          "login": "octocat",
          "name": "The Octocat",
          "bio": "tentacular",
          "public_repos": 8,
          "followers": 100,
          "created_at": "2011-01-25T18:44:36Z"
        }
        """;

    [Fact]
    public async Task GetUserAsync_OnHttp200_ReturnsParsedUser()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidUserJson, Encoding.UTF8, "application/json")
        };
        var sut = CreateSut(response, out var handler);

        // Act
        var user = await sut.GetUserAsync("octocat");

        // Assert
        user.Login.Should().Be("octocat");
        user.Name.Should().Be("The Octocat");
        user.Bio.Should().Be("tentacular");
        user.PublicRepos.Should().Be(8);
        user.Followers.Should().Be(100);
        user.CreatedAt.Should().Be(new DateTimeOffset(2011, 1, 25, 18, 44, 36, TimeSpan.Zero));
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("users/octocat");
    }

    [Fact]
    public async Task GetUserAsync_EscapesLoginInRequestUri()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidUserJson, Encoding.UTF8, "application/json")
        };
        var sut = CreateSut(response, out var handler);

        // Act
        _ = await sut.GetUserAsync("foo bar");

        // Assert
        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().EndWith("users/foo%20bar");
    }

    [Fact]
    public async Task GetUserAsync_OnHttp404_ThrowsGitHubUserNotFoundException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"message":"Not Found"}""", Encoding.UTF8, "application/json")
        };
        var sut = CreateSut(response, out _);

        // Act
        var act = async () => await sut.GetUserAsync("ghost");

        // Assert
        var ex = await act.Should().ThrowAsync<GitHubUserNotFoundException>();
        ex.Which.Login.Should().Be("ghost");
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task GetUserAsync_OnHttpFailure_ThrowsGitHubApiException(HttpStatusCode status)
    {
        // Arrange
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent("oops", Encoding.UTF8, "text/plain")
        };
        var sut = CreateSut(response, out _);

        // Act
        var act = async () => await sut.GetUserAsync("octocat");

        // Assert
        var ex = await act.Should().ThrowAsync<GitHubApiException>();
        ex.Which.StatusCode.Should().Be(status);
        ex.Which.Should().NotBeOfType<GitHubUserNotFoundException>();
        ex.Which.Message.Should().Contain("oops");
    }

    [Fact]
    public async Task GetUserAsync_OnEmptyResponseBody_ThrowsGitHubApiException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };
        var sut = CreateSut(response, out _);

        // Act
        var act = async () => await sut.GetUserAsync("octocat");

        // Assert
        var ex = await act.Should().ThrowAsync<GitHubApiException>();
        ex.Which.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task GetUserAsync_WithWhitespaceLogin_Throws()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK), out _);

        // Act
        var act = async () => await sut.GetUserAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static GitHubUsersClient CreateSut(
        HttpResponseMessage response,
        out StubHttpMessageHandler handler)
    {
        handler = new StubHttpMessageHandler(response);
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        return new GitHubUsersClient(http, NullLogger<GitHubUsersClient>.Instance);
    }
}
