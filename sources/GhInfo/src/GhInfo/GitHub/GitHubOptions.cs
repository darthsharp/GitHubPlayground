using System.ComponentModel.DataAnnotations;

namespace GhInfo.GitHub;

/// <summary>
/// Configuration for the GitHub REST API client.
/// </summary>
public sealed class GitHubOptions
{
    /// <summary>
    /// Gets the configuration section name (<c>GitHub</c>) used by <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "GitHub";

    /// <summary>
    /// Gets or sets the base address of the GitHub REST API.
    /// </summary>
    /// <value>An absolute URL ending with a trailing slash, for example <c>https://api.github.com/</c>.</value>
    [Required]
    [Url]
    public required string BaseAddress { get; init; }

    /// <summary>
    /// Gets or sets the <c>User-Agent</c> header sent with every request.
    /// </summary>
    /// <remarks>
    /// GitHub requires every request to include a <c>User-Agent</c> header; requests without one
    /// are rejected with HTTP 403.
    /// </remarks>
    [Required]
    public required string UserAgent { get; init; }
}
