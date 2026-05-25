using CreativeCoders.Core;
using GhInfo.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GhInfo.Caching;

/// <summary>
/// SQLite-backed implementation of <see cref="IUserCacheService"/>.
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

    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GitHubUser?> TryGetAsync(string login, CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        var key = NormalizeLogin(login);
        var entry = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == key, cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            _logger.LogDebug("Cache miss for {Login}", key);

            return null;
        }

        var age = _timeProvider.GetUtcNow() - entry.CachedAt;
        if (age > _options.TimeToLive)
        {
            _logger.LogDebug("Cache entry for {Login} expired ({Age} > {Ttl})", key, age, _options.TimeToLive);

            return null;
        }

        _logger.LogInformation("Cache hit for {Login} (age {Age})", key, age);

        return new GitHubUser
        {
            Login = entry.Login,
            Name = entry.Name,
            Bio = entry.Bio,
            PublicRepos = entry.PublicRepos,
            Followers = entry.Followers,
            CreatedAt = entry.CreatedAt
        };
    }

    /// <inheritdoc/>
    public async Task SetAsync(GitHubUser user, CancellationToken cancellationToken = default)
    {
        Ensure.NotNull(user);

        var key = NormalizeLogin(user.Login);
        var existing = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Login == key, cancellationToken)
            .ConfigureAwait(false);

        var now = _timeProvider.GetUtcNow();

        if (existing is null)
        {
            _dbContext.Users.Add(new CachedUser
            {
                Login = key,
                Name = user.Name,
                Bio = user.Bio,
                PublicRepos = user.PublicRepos,
                Followers = user.Followers,
                CreatedAt = user.CreatedAt,
                CachedAt = now
            });
        }
        else
        {
            existing.Name = user.Name;
            existing.Bio = user.Bio;
            existing.PublicRepos = user.PublicRepos;
            existing.Followers = user.Followers;
            existing.CreatedAt = user.CreatedAt;
            existing.CachedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cached GitHub user {Login}", key);
    }

    private static string NormalizeLogin(string login)
    {
        return login.Trim().ToLowerInvariant();
    }
}
