using System.Text.Json.Serialization;

namespace GhInfo.GitHub;

/// <summary>
/// Represents the subset of public GitHub user fields consumed by <c>gh-info</c>.
/// </summary>
public sealed class GitHubUser
{
    /// <summary>
    /// Gets the user's login name (handle).
    /// </summary>
    [JsonPropertyName("login")]
    public required string Login { get; init; }

    /// <summary>
    /// Gets the user's display name, or <see langword="null"/> if not set.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the user's profile biography, or <see langword="null"/> if not set.
    /// </summary>
    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    /// <summary>
    /// Gets the number of public repositories owned by the user.
    /// </summary>
    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; init; }

    /// <summary>
    /// Gets the number of followers of the user.
    /// </summary>
    [JsonPropertyName("followers")]
    public int Followers { get; init; }

    /// <summary>
    /// Gets the UTC timestamp at which the account was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}
