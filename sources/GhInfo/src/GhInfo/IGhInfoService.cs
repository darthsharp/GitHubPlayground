using GhInfo.GitHub;

namespace GhInfo;

/// <summary>
/// Resolves a GitHub user, optionally consulting the local cache before calling the API.
/// </summary>
public interface IGhInfoService
{
    /// <summary>
    /// Returns a GitHub user, using the cache when allowed and falling back to the API.
    /// </summary>
    /// <remarks>
    /// <paramref name="useCache"/> only controls the read path. On a fresh API response the
    /// cache is always refreshed, regardless of the flag's value.
    /// </remarks>
    /// <param name="login">The GitHub login to look up.</param>
    /// <param name="useCache">
    /// When <see langword="true"/>, a non-expired cached entry is returned without contacting the API.
    /// When <see langword="false"/>, the cache read is skipped and the API is called; the
    /// fresh result is still written back to the cache.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The resolved GitHub user and a flag indicating whether the cache was used.</returns>
    /// <exception cref="GitHubUserNotFoundException">Thrown when the user does not exist.</exception>
    /// <exception cref="GitHubApiException">Thrown when the GitHub API returns an unexpected response.</exception>
    Task<GhInfoResult> GetUserAsync(string login, bool useCache, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a <see cref="IGhInfoService.GetUserAsync(string, bool, CancellationToken)"/> call.
/// </summary>
/// <param name="User">The resolved GitHub user.</param>
/// <param name="FromCache">A value indicating whether the user was returned from the cache.</param>
public sealed record GhInfoResult(GitHubUser User, bool FromCache);
