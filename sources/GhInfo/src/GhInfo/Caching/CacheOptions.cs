using System.ComponentModel.DataAnnotations;

namespace GhInfo.Caching;

/// <summary>
/// Configuration for the local SQLite-backed cache.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Gets the configuration section name (<c>Cache</c>) used by <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// Gets or sets how long a cached entry remains valid, in minutes.
    /// </summary>
    /// <value>Defaults to <c>15</c> per requirement.</value>
    [Range(1, 1440)]
    public int TimeToLiveMinutes { get; init; } = 15;

    /// <summary>
    /// Gets the cache time-to-live as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan TimeToLive => TimeSpan.FromMinutes(TimeToLiveMinutes);
}
