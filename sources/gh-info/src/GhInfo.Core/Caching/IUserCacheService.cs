using GhInfo.Core.Models;

namespace GhInfo.Core.Caching;

/// <summary>
/// Provides read and write access to the local cache of GitHub user profiles.
/// </summary>
public interface IUserCacheService
{
    /// <summary>
    /// Returns the cached profile for the given login if a non-expired entry exists.
    /// </summary>
    /// <param name="login">The login (handle) of the user to look up.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// The cached <see cref="GitHubUser"/> when a fresh entry exists; otherwise <see langword="null"/>.
    /// </returns>
    Task<GitHubUser?> GetAsync(string login, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the cached profile for the given user, stamping it with the current time.
    /// </summary>
    /// <param name="user">The user profile to cache.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    Task SetAsync(GitHubUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries whose age exceeds the configured expiration window.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The number of expired entries that were removed.</returns>
    Task<int> PruneExpiredAsync(CancellationToken cancellationToken = default);
}
