namespace GhInfo.Core.Models;

/// <summary>
/// Represents the public profile information of a GitHub user as returned by the
/// <c>GET /users/{login}</c> endpoint of the GitHub REST API.
/// </summary>
public sealed class GitHubUser
{
    /// <summary>Gets the unique login (handle) of the user, for example <c>octocat</c>.</summary>
    public string Login { get; init; } = string.Empty;

    /// <summary>Gets the display name of the user, or <see langword="null"/> if none is set.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the biography text of the user, or <see langword="null"/> if none is set.</summary>
    public string? Bio { get; init; }

    /// <summary>Gets the number of public repositories owned by the user.</summary>
    public int PublicRepos { get; init; }

    /// <summary>Gets the number of followers of the user.</summary>
    public int Followers { get; init; }

    /// <summary>Gets the UTC timestamp at which the user account was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
