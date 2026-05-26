using CreativeCoders.Core;
using GhInfo.Caching;
using GhInfo.GitHub;
using Microsoft.Extensions.Logging;

namespace GhInfo;

/// <summary>
/// Default <see cref="IGhInfoService"/> implementation that combines the
/// <see cref="IGitHubUsersClient"/> with the local <see cref="IUserCacheService"/>.
/// </summary>
public sealed class GhInfoService(
    IGitHubUsersClient gitHubUsersClient,
    IUserCacheService userCacheService,
    ILogger<GhInfoService> logger) : IGhInfoService
{
    private readonly IGitHubUsersClient _gitHubUsersClient = Ensure.NotNull(gitHubUsersClient);
    private readonly IUserCacheService _userCacheService = Ensure.NotNull(userCacheService);
    private readonly ILogger<GhInfoService> _logger = Ensure.NotNull(logger);

    /// <inheritdoc />
    public async Task<GitHubUser?> GetUserAsync(string login, bool useCache, CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        if (useCache)
        {
            var cached = await _userCacheService
                .GetAsync(login, cancellationToken)
                .ConfigureAwait(false);

            if (cached is not null)
            {
                _logger.LogInformation("Returning cached profile for {Login}", login);

                return cached;
            }
        }
        else
        {
            _logger.LogInformation("Cache bypass requested for {Login}", login);
        }

        var user = await _gitHubUsersClient
            .GetUserAsync(login, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return null;
        }

        if (useCache)
        {
            await _userCacheService
                .SetAsync(user, cancellationToken)
                .ConfigureAwait(false);
        }

        return user;
    }
}
