using System.Text.Json.Serialization;

namespace GhInfo.GitHub;

/// <summary>
/// Snapshot of the public profile fields returned by the GitHub
/// <c>GET /users/{username}</c> endpoint.
/// </summary>
/// <param name="Login">The unique account login of the user.</param>
/// <param name="Name">The display name of the user, or <see langword="null"/> if not set.</param>
/// <param name="Bio">The user's biography text, or <see langword="null"/> if not set.</param>
/// <param name="PublicRepos">The number of public repositories owned by the user.</param>
/// <param name="Followers">The number of followers the user has.</param>
/// <param name="CreatedAt">The point in time at which the account was created.</param>
public sealed record GitHubUser(
    [property: JsonPropertyName("login")] string Login,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("bio")] string? Bio,
    [property: JsonPropertyName("public_repos")] int PublicRepos,
    [property: JsonPropertyName("followers")] int Followers,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);
