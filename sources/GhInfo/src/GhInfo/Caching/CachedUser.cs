namespace GhInfo.Caching;

/// <summary>
/// Persistent representation of a GitHub user snapshot stored in the local SQLite cache.
/// </summary>
public sealed class CachedUser
{
    /// <summary>
    /// Gets the GitHub login, used as the primary key of the cache entry.
    /// </summary>
    /// <value>The lower-cased, unique GitHub account login.</value>
    public required string Login { get; init; }

    /// <summary>
    /// Gets the user's display name at the time the snapshot was captured.
    /// </summary>
    /// <value>The display name, or <see langword="null"/> if GitHub did not return one.</value>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the user's biography text at the time the snapshot was captured.
    /// </summary>
    /// <value>The biography, or <see langword="null"/> if not set on the account.</value>
    public string? Bio { get; init; }

    /// <summary>
    /// Gets the number of public repositories the user owned when the snapshot was captured.
    /// </summary>
    /// <value>A non-negative integer.</value>
    public int PublicRepos { get; init; }

    /// <summary>
    /// Gets the number of followers the user had when the snapshot was captured.
    /// </summary>
    /// <value>A non-negative integer.</value>
    public int Followers { get; init; }

    /// <summary>
    /// Gets the timestamp at which the GitHub account was created.
    /// </summary>
    /// <value>The account creation timestamp reported by GitHub.</value>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp at which this snapshot was written to the cache.
    /// </summary>
    /// <value>A UTC instant; used to determine cache freshness.</value>
    public DateTimeOffset CachedAt { get; init; }
}
