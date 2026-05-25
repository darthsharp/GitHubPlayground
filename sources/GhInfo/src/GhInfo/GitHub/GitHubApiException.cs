using System.Net;

namespace GhInfo.GitHub;

/// <summary>
/// Thrown when the GitHub REST API returns an unexpected response.
/// </summary>
public class GitHubApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubApiException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by the API.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public GitHubApiException(HttpStatusCode statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }
}

/// <summary>
/// Thrown when the requested GitHub user does not exist (HTTP 404).
/// </summary>
public sealed class GitHubUserNotFoundException : GitHubApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubUserNotFoundException"/> class.
    /// </summary>
    /// <param name="login">The login that could not be resolved.</param>
    public GitHubUserNotFoundException(string login)
        : base(HttpStatusCode.NotFound, $"GitHub user '{login}' was not found.")
    {
        Login = login;
    }

    /// <summary>
    /// Gets the login that was requested.
    /// </summary>
    public string Login { get; }
}
