using System.ComponentModel.DataAnnotations;

namespace GhInfo.GitHub;

/// <summary>
/// Strongly-typed options for the GitHub REST API client.
/// </summary>
public sealed class GitHubOptions
{
    /// <summary>
    /// Name of the configuration section that binds to this options class.
    /// </summary>
    public const string SectionName = "GitHub";

    /// <summary>
    /// Gets the base address of the GitHub REST API.
    /// </summary>
    /// <value>An absolute HTTPS URL, typically <c>https://api.github.com/</c>.</value>
    [Required]
    [Url]
    public required string BaseAddress { get; init; }

    /// <summary>
    /// Gets the value sent in the <c>User-Agent</c> HTTP request header.
    /// </summary>
    /// <value>A non-empty product identifier; GitHub requires every request to carry one.</value>
    [Required]
    public required string UserAgent { get; init; }

    /// <summary>
    /// Gets the optional personal access token used to authenticate requests.
    /// </summary>
    /// <value>
    /// A GitHub PAT (classic or fine-grained) for authenticated calls, or
    /// <see langword="null"/> for anonymous access subject to the lower
    /// rate-limit budget.
    /// </value>
    public string? Token { get; init; }
}
