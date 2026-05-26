using System.ComponentModel.DataAnnotations;

namespace GhInfo.Caching;

/// <summary>
/// Strongly-typed options for the local SQLite-backed user cache.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Name of the configuration section that binds to this options class.
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Default sub-directory used under the user's local application data folder
    /// when no explicit <see cref="DatabasePath"/> is configured.
    /// </summary>
    public const string DefaultDirectoryName = "gh-info";

    /// <summary>
    /// Default file name of the cache database.
    /// </summary>
    public const string DefaultDatabaseFileName = "cache.db";

    /// <summary>
    /// Gets the duration, in minutes, for which a cached user entry is considered fresh.
    /// </summary>
    /// <value>A positive integer; defaults to <c>15</c> minutes.</value>
    [Range(1, 24 * 60)]
    public int DurationMinutes { get; init; } = 15;

    /// <summary>
    /// Gets the optional override path of the SQLite cache database file.
    /// </summary>
    /// <value>
    /// An absolute or relative file system path, or <see langword="null"/> to use the
    /// platform-default path under <see cref="Environment.SpecialFolder.LocalApplicationData"/>.
    /// </value>
    public string? DatabasePath { get; init; }

    /// <summary>
    /// Computes the effective path of the SQLite cache database, falling back to a
    /// platform-default location when <see cref="DatabasePath"/> is not set.
    /// </summary>
    /// <returns>The absolute file system path that should host the cache database.</returns>
    public string GetDatabasePath()
    {
        if (!string.IsNullOrWhiteSpace(DatabasePath))
        {
            return DatabasePath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(localAppData, DefaultDirectoryName, DefaultDatabaseFileName);
    }
}
