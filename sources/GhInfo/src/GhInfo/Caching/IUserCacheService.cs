using GhInfo.GitHub;

namespace GhInfo.Caching;

/// <summary>
/// Stores and retrieves <see cref="GitHubUser"/> values from the local cache.
/// </summary>
public interface IUserCacheService
{
    /// <summary>
    /// Ensures the underlying cache database exists and is reachable.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the cached entry for <paramref name="login"/> if it exists and has not expired.
    /// </summary>
    /// <param name="login">The GitHub login (case-insensitive) to look up.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The cached user, or <see langword="null"/> if the cache miss or the entry has expired.</returns>
    Task<GitHubUser?> TryGetAsync(string login, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores <paramref name="user"/> in the cache, overwriting any existing entry for the same login.
    /// </summary>
    /// <param name="user">The user to cache.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task SetAsync(GitHubUser user, CancellationToken cancellationToken = default);
}
