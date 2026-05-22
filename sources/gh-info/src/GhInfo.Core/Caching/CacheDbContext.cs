using Microsoft.EntityFrameworkCore;

namespace GhInfo.Core.Caching;

/// <summary>
/// The Entity Framework Core database context backing the local GitHub user cache.
/// </summary>
/// <param name="options">The options used to configure the context.</param>
public sealed class CacheDbContext(DbContextOptions<CacheDbContext> options) : DbContext(options)
{
    /// <summary>Gets the set of cached GitHub user profiles.</summary>
    public DbSet<CachedUser> Users => Set<CachedUser>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<CachedUser>();
        user.ToTable("CachedUsers");
        user.HasKey(x => x.Login);
        user.Property(x => x.Login).IsRequired();
        user.Property(x => x.FetchedAt).IsRequired();
    }
}
