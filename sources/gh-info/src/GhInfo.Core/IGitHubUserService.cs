namespace GhInfo.Core;

/// <summary>
/// Coordinates retrieval of GitHub user profiles, serving fresh entries from the local cache and
/// falling back to the GitHub REST API when needed.
/// </summary>
public interface IGitHubUserService
{
    /// <summary>
    /// Resolves the profile for the given login, using the cache when permitted and fresh.
    /// </summary>
    /// <param name="login">The login (handle) of the user to resolve.</param>
    /// <param name="useCache">
    /// <see langword="true"/> to consult the cache before calling the API; <see langword="false"/> to
    /// always fetch a fresh copy from the API (the cache is still refreshed with the result).
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>The resolved profile and an indication of whether it came from the cache.</returns>
    Task<UserInfoResult> GetUserAsync(
        string login,
        bool useCache = true,
        CancellationToken cancellationToken = default);
}
