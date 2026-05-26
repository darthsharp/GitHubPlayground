using GhInfo.GitHub;

namespace GhInfo.Caching;

/// <summary>
/// Provides time-limited persistent caching of GitHub user snapshots.
/// </summary>
public interface IUserCacheService
{
    /// <summary>
    /// Reads a cached GitHub user snapshot, returning <see langword="null"/> when
    /// no entry exists or when the entry has expired.
    /// </summary>
    /// <param name="login">The GitHub account login (case-insensitive).</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The fresh cached snapshot for <paramref name="login"/>, or <see langword="null"/>.</returns>
    Task<GitHubUser?> GetAsync(string login, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the cached snapshot for the supplied GitHub user, stamping
    /// the entry with the current time.
    /// </summary>
    /// <param name="user">The GitHub user snapshot to persist.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that completes when the entry has been persisted.</returns>
    Task SetAsync(GitHubUser user, CancellationToken cancellationToken = default);
}
