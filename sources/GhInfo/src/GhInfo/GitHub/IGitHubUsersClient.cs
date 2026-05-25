namespace GhInfo.GitHub;

/// <summary>
/// Abstraction over the GitHub <c>/users/{login}</c> REST endpoint.
/// </summary>
public interface IGitHubUsersClient
{
    /// <summary>
    /// Fetches the public profile of a GitHub user.
    /// </summary>
    /// <param name="login">The GitHub login (handle) to look up.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The user's public profile data.</returns>
    /// <exception cref="GitHubUserNotFoundException">Thrown when the user does not exist.</exception>
    /// <exception cref="GitHubApiException">Thrown when the API returns an unexpected response.</exception>
    Task<GitHubUser> GetUserAsync(string login, CancellationToken cancellationToken = default);
}
