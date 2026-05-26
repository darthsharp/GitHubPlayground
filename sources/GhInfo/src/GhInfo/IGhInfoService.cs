using GhInfo.GitHub;

namespace GhInfo;

/// <summary>
/// Orchestrates lookups of GitHub user profiles, combining the remote GitHub
/// REST API with the local SQLite cache.
/// </summary>
public interface IGhInfoService
{
    /// <summary>
    /// Resolves the public GitHub profile for the supplied login, honoring the
    /// local cache when <paramref name="useCache"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="login">The GitHub account login.</param>
    /// <param name="useCache">
    /// <see langword="true"/> to consult the local cache first and to persist
    /// fresh API results into it; <see langword="false"/> to bypass the cache
    /// in both directions.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The resolved <see cref="GitHubUser"/>, or <see langword="null"/> if no such user exists.</returns>
    Task<GitHubUser?> GetUserAsync(string login, bool useCache, CancellationToken cancellationToken = default);
}
