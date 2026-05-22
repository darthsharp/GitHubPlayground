namespace GhInfo.Core.Caching;

/// <summary>
/// Options that control the local SQLite cache of GitHub user profiles.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>The configuration section name these options are bound from by default.</summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Gets or sets the maximum age a cached entry may have before it is considered stale and
    /// re-fetched from the API. Defaults to 15 minutes.
    /// </summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the file system path of the SQLite database file used for caching.
    /// Defaults to <c>gh-info-cache.db</c> in the current working directory.
    /// </summary>
    public string DatabasePath { get; set; } = "gh-info-cache.db";
}
