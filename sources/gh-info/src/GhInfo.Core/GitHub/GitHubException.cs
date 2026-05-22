using System.Net;

namespace GhInfo.Core.GitHub;

/// <summary>
/// The exception thrown when a request to the GitHub REST API fails.
/// </summary>
public class GitHubException : Exception
{
    /// <summary>Gets the HTTP status code returned by the API, if the failure was an HTTP error.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>Gets the raw response body returned by the API, if available.</summary>
    public string? ResponseBody { get; }

    /// <summary>Initializes a new instance of the <see cref="GitHubException"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public GitHubException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GitHubException"/> class for an HTTP error.</summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    /// <param name="responseBody">The raw response body returned by the API, if available.</param>
    public GitHubException(HttpStatusCode statusCode, string? responseBody = null)
        : base($"GitHub API returned {(int)statusCode} {statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

/// <summary>
/// The exception thrown when the requested GitHub user does not exist (HTTP 404).
/// </summary>
public sealed class GitHubUserNotFoundException : GitHubException
{
    /// <summary>Gets the login of the user that could not be found.</summary>
    public string Login { get; }

    /// <summary>Initializes a new instance of the <see cref="GitHubUserNotFoundException"/> class.</summary>
    /// <param name="login">The login of the user that could not be found.</param>
    /// <param name="responseBody">The raw response body returned by the API, if available.</param>
    public GitHubUserNotFoundException(string login, string? responseBody = null)
        : base(HttpStatusCode.NotFound, responseBody)
    {
        Login = login;
    }
}

/// <summary>
/// The exception thrown when the GitHub API rate limit has been exceeded (HTTP 403 or 429).
/// </summary>
public sealed class GitHubRateLimitException : GitHubException
{
    /// <summary>Initializes a new instance of the <see cref="GitHubRateLimitException"/> class.</summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    /// <param name="responseBody">The raw response body returned by the API, if available.</param>
    public GitHubRateLimitException(HttpStatusCode statusCode, string? responseBody = null)
        : base(statusCode, responseBody)
    {
    }
}
