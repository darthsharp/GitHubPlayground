namespace GhInfo.Core.Caching;

/// <summary>
/// Represents a cached GitHub user profile persisted in the local SQLite database, together with
/// the timestamp at which it was fetched from the API.
/// </summary>
public sealed class CachedUser
{
    /// <summary>Gets or sets the login (handle) of the user. This is the primary key.</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the user, if any.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the biography text of the user, if any.</summary>
    public string? Bio { get; set; }

    /// <summary>Gets or sets the number of public repositories owned by the user.</summary>
    public int PublicRepos { get; set; }

    /// <summary>Gets or sets the number of followers of the user.</summary>
    public int Followers { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the user account was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which this entry was fetched from the API.</summary>
    public DateTimeOffset FetchedAt { get; set; }
}
