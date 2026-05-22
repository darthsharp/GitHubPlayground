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

        var normalized = login.ToLowerInvariant();

        var entry = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Login.ToLower() == normalized, cancellationToken)
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
