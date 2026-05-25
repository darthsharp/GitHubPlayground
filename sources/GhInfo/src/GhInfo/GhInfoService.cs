using CreativeCoders.Core;
using GhInfo.Caching;
using GhInfo.GitHub;
using Microsoft.Extensions.Logging;

namespace GhInfo;

/// <summary>
/// Orchestrates cache and HTTP lookups to resolve a GitHub user.
/// </summary>
public sealed class GhInfoService(
    IGitHubUsersClient client,
    IUserCacheService cache,
    ILogger<GhInfoService> logger)
{
    private readonly IGitHubUsersClient _client = Ensure.NotNull(client);
    private readonly IUserCacheService _cache = Ensure.NotNull(cache);
    private readonly ILogger<GhInfoService> _logger = Ensure.NotNull(logger);

    /// <summary>
    /// Returns a GitHub user, using the cache when allowed and falling back to the API.
    /// </summary>
    /// <remarks>
    /// <paramref name="useCache"/> only controls the read path. On a fresh API response the
    /// cache is always refreshed, regardless of the flag's value. To clear the cache,
    /// delete the SQLite file at <c>%LOCALAPPDATA%/gh-info/cache.db</c>.
    /// </remarks>
    /// <param name="login">The GitHub login to look up.</param>
    /// <param name="useCache">
    /// When <see langword="true"/>, a non-expired cached entry is returned without contacting the API.
    /// When <see langword="false"/>, the cache read is skipped and the API is called; the
    /// fresh result is still written back to the cache.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The resolved GitHub user and a flag indicating whether the cache was used.</returns>
    public async Task<GhInfoResult> GetUserAsync(
        string login,
        bool useCache,
        CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        if (useCache)
        {
            var cached = await _cache.TryGetAsync(login, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return new GhInfoResult(cached, FromCache: true);
            }
        }
        else
        {
            _logger.LogInformation("Bypassing cache for {Login} (--no-cache)", login);
        }

        var user = await _client.GetUserAsync(login, cancellationToken).ConfigureAwait(false);
        await _cache.SetAsync(user, cancellationToken).ConfigureAwait(false);

        return new GhInfoResult(user, FromCache: false);
    }
}

/// <summary>
/// Result of a <see cref="GhInfoService.GetUserAsync(string, bool, CancellationToken)"/> call.
/// </summary>
/// <param name="User">The resolved GitHub user.</param>
/// <param name="FromCache">A value indicating whether the user was returned from the cache.</param>
public sealed record GhInfoResult(GitHubUser User, bool FromCache);
