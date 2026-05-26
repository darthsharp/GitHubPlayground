using System.Net;

namespace GhInfo.GitHub;

/// <summary>
/// Exception raised when the GitHub REST API responds with a non-success
/// status code that the client cannot translate into a normal result.
/// </summary>
public sealed class GitHubApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubApiException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by GitHub.</param>
    /// <param name="responseBody">The raw response body, used for diagnostics.</param>
    /// <param name="message">A human-readable description of the failure.</param>
    public GitHubApiException(HttpStatusCode statusCode, string? responseBody, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Gets the HTTP status code returned by the GitHub API.
    /// </summary>
    /// <value>The status code of the failing HTTP response.</value>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the response body returned by GitHub, if any.
    /// </summary>
    /// <value>The raw response body, or <see langword="null"/> when the response had no body.</value>
    public string? ResponseBody { get; }
}
