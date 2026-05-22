using CreativeCoders.Core;
using GhInfo.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GhInfo.Core.Caching;

/// <summary>
/// SQLite-backed implementation of <see cref="IUserCacheService"/> using Entity Framework Core.
/// </summary>
internal sealed class UserCacheService(
    CacheDbContext dbContext,
    IOptions<CacheOptions> options,
    TimeProvider timeProvider,
    ILogger<UserCacheService> logger) : IUserCacheService
{
    private readonly CacheDbContext _dbContext = Ensure.NotNull(dbContext);
    private readonly CacheOptions _options = Ensure.NotNull(options).Value;
    private readonly TimeProvider _timeProvider = Ensure.NotNull(timeProvider);
    private readonly ILogger<UserCacheService> _logger = Ensure.NotNull(logger);

    /// <inheritdoc />
    public async Task<GitHubUser?> GetAsync(string login, CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        // The Login column uses a NOCASE collation, so this exact comparison matches case-insensitively
        // and is served by the primary-key index.
        var entry = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Login == login, cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            _logger.LogDebug("Cache miss for user {Login}", login);

            return null;
        }

        var age = _timeProvider.GetUtcNow() - entry.FetchedAt;
        if (age > _options.Expiration)
        {
            _logger.LogDebug(
                "Cache entry for user {Login} is stale (age {Age}, expiration {Expiration})",
                login,
                age,
                _options.Expiration);

            return null;
        }

        _logger.LogInformation("Cache hit for user {Login} (age {Age})", login, age);

        return ToModel(entry);
    }

    /// <inheritdoc />
    public async Task SetAsync(GitHubUser user, CancellationToken cancellationToken = default)
    {
        Ensure.NotNull(user);

        var entry = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Login == user.Login, cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            entry = new CachedUser { Login = user.Login };
            _dbContext.Users.Add(entry);
        }

        entry.Name = user.Name;
        entry.Bio = user.Bio;
        entry.PublicRepos = user.PublicRepos;
        entry.Followers = user.Followers;
        entry.CreatedAt = user.CreatedAt;
        entry.FetchedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Cached user {Login}", user.Login);
    }

    /// <inheritdoc />
    public async Task<int> PruneExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _timeProvider.GetUtcNow() - _options.Expiration;

        // SQLite cannot translate a DateTimeOffset comparison inside a bulk delete, so the age check
        // is evaluated client-side (as in GetAsync) and expired rows are then deleted by their key.
        var allEntries = await _dbContext.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var expiredLogins = allEntries
            .Where(x => x.FetchedAt < cutoff)
            .Select(x => x.Login)
            .ToList();

        if (expiredLogins.Count == 0)
        {
            return 0;
        }

        var removed = await _dbContext.Users
            .Where(x => expiredLogins.Contains(x.Login))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Pruned {Count} expired cache entries", removed);

        return removed;
    }

    private static GitHubUser ToModel(CachedUser entry)
    {
        return new GitHubUser
        {
            Login = entry.Login,
            Name = entry.Name,
            Bio = entry.Bio,
            PublicRepos = entry.PublicRepos,
            Followers = entry.Followers,
            CreatedAt = entry.CreatedAt,
        };
    }
}
