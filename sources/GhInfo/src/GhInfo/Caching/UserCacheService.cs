using CreativeCoders.Core;
using GhInfo.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GhInfo.Caching;

/// <summary>
/// Default <see cref="IUserCacheService"/> implementation that persists snapshots
/// via Entity Framework Core in a local SQLite database.
/// </summary>
public sealed class UserCacheService(
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

        var key = NormalizeLogin(login);

        var entry = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Login == key, cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            _logger.LogDebug("Cache miss for {Login}", key);

            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var age = now - entry.CachedAt;
        var ttl = TimeSpan.FromMinutes(_options.DurationMinutes);

        if (age >= ttl)
        {
            _logger.LogDebug("Cache entry for {Login} is stale (age {Age}, ttl {Ttl})", key, age, ttl);

            return null;
        }

        _logger.LogDebug("Cache hit for {Login} (age {Age})", key, age);

        return new GitHubUser(
            entry.Login,
            entry.Name,
            entry.Bio,
            entry.PublicRepos,
            entry.Followers,
            entry.CreatedAt);
    }

    /// <inheritdoc />
    public async Task SetAsync(GitHubUser user, CancellationToken cancellationToken = default)
    {
        Ensure.NotNull(user);

        var key = NormalizeLogin(user.Login);
        var now = _timeProvider.GetUtcNow();

        var existing = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Login == key, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            _dbContext.Users.Remove(existing);
        }

        _dbContext.Users.Add(new CachedUser
        {
            Login = key,
            Name = user.Name,
            Bio = user.Bio,
            PublicRepos = user.PublicRepos,
            Followers = user.Followers,
            CreatedAt = user.CreatedAt,
            CachedAt = now,
        });

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Stored cache entry for {Login} at {CachedAt}", key, now);
    }

    private static string NormalizeLogin(string login)
    {
        return login.Trim().ToLowerInvariant();
    }
}
