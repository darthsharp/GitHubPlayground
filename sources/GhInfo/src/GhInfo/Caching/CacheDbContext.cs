using Microsoft.EntityFrameworkCore;

namespace GhInfo.Caching;

/// <summary>
/// EF Core <see cref="DbContext"/> backing the local SQLite cache.
/// </summary>
public sealed class CacheDbContext(DbContextOptions<CacheDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the set of cached GitHub user lookups.
    /// </summary>
    public DbSet<CachedUser> Users => Set<CachedUser>();
}
