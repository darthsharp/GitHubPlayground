using CreativeCoders.Core;
using GhInfo.Core.Caching;
using GhInfo.Core.GitHub;
using Microsoft.Extensions.Logging;

namespace GhInfo.Core;

/// <summary>
/// Default implementation of <see cref="IGitHubUserService"/> that combines the local cache with
/// the <see cref="IGitHubUsersClient"/> typed HTTP client.
/// </summary>
internal sealed class GitHubUserService(
    IGitHubUsersClient client,
    IUserCacheService cache,
    ILogger<GitHubUserService> logger) : IGitHubUserService
{
    private readonly IGitHubUsersClient _client = Ensure.NotNull(client);
    private readonly IUserCacheService _cache = Ensure.NotNull(cache);
    private readonly ILogger<GitHubUserService> _logger = Ensure.NotNull(logger);

    /// <inheritdoc />
    public async Task<UserInfoResult> GetUserAsync(
        string login,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        if (useCache)
        {
            var cached = await _cache.GetAsync(login, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return new UserInfoResult(cached, FromCache: true);
            }
        }
        else
        {
            _logger.LogDebug("Cache bypassed for user {Login} (--no-cache)", login);
        }

        var user = await _client.GetUserAsync(login, cancellationToken).ConfigureAwait(false);

        await _cache.SetAsync(user, cancellationToken).ConfigureAwait(false);

        return new UserInfoResult(user, FromCache: false);
    }
}
