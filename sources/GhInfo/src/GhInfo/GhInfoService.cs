using CreativeCoders.Core;
using GhInfo.Caching;
using GhInfo.GitHub;
using Microsoft.Extensions.Logging;

namespace GhInfo;

/// <summary>
/// Default implementation of <see cref="IGhInfoService"/> that orchestrates cache and HTTP lookups.
/// </summary>
public sealed class GhInfoService(
    IGitHubUsersClient client,
    IUserCacheService cache,
    ILogger<GhInfoService> logger) : IGhInfoService
{
    private readonly IGitHubUsersClient _client = Ensure.NotNull(client);
    private readonly IUserCacheService _cache = Ensure.NotNull(cache);
    private readonly ILogger<GhInfoService> _logger = Ensure.NotNull(logger);

    /// <inheritdoc/>
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
