namespace GhInfo.Core.GitHub;

/// <summary>
/// Options for configuring the <see cref="IGitHubUsersClient"/> typed HTTP client.
/// </summary>
public sealed class GitHubClientOptions
{
    /// <summary>The configuration section name these options are bound from by default.</summary>
    public const string SectionName = "GitHub";

    /// <summary>
    /// Gets or sets the base address of the GitHub REST API. Defaults to <c>https://api.github.com/</c>.
    /// </summary>
    public string BaseAddress { get; set; } = "https://api.github.com/";

    /// <summary>
    /// Gets or sets the value of the <c>User-Agent</c> header sent with every request.
    /// The GitHub API rejects requests without a user agent. Defaults to <c>gh-info</c>.
    /// </summary>
    public string UserAgent { get; set; } = "gh-info";

    /// <summary>
    /// Gets or sets an optional personal access token used for authenticated requests, which
    /// raises the API rate limit. When <see langword="null"/> or empty, requests are anonymous.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>Gets or sets the request timeout. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
