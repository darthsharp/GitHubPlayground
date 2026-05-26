using Microsoft.EntityFrameworkCore;

namespace GhInfo.Caching;

/// <summary>
/// Entity Framework Core <see cref="DbContext"/> backing the local SQLite cache
/// of GitHub user snapshots.
/// </summary>
public sealed class CacheDbContext(DbContextOptions<CacheDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the set of cached GitHub user snapshots.
    /// </summary>
    /// <value>An EF Core <see cref="DbSet{TEntity}"/> for <see cref="CachedUser"/>.</value>
    public DbSet<CachedUser> Users => Set<CachedUser>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<CachedUser>();
        user.ToTable("CachedUsers");
        user.HasKey(x => x.Login);
        user.Property(x => x.Login).HasMaxLength(64);
        user.Property(x => x.Name).HasMaxLength(255);
        user.Property(x => x.Bio).HasMaxLength(1024);
    }
}
