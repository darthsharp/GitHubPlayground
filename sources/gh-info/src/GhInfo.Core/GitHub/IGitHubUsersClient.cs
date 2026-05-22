using GhInfo.Core.Models;

namespace GhInfo.Core.GitHub;

/// <summary>
/// Provides access to the user-related endpoints of the GitHub REST API.
/// </summary>
public interface IGitHubUsersClient
{
    /// <summary>
    /// Retrieves the public profile of the GitHub user with the given login.
    /// </summary>
    /// <param name="login">The login (handle) of the user to retrieve.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the request to complete.</param>
    /// <returns>The public profile of the requested user.</returns>
    /// <exception cref="GitHubUserNotFoundException">The user does not exist.</exception>
    /// <exception cref="GitHubRateLimitException">The API rate limit has been exceeded.</exception>
    /// <exception cref="GitHubException">The API returned another error response.</exception>
    Task<GitHubUser> GetUserAsync(string login, CancellationToken cancellationToken = default);
}
