namespace GhInfo.GitHub;

/// <summary>
/// Abstraction over the GitHub <c>/users/{username}</c> REST endpoint that
/// returns public profile information.
/// </summary>
public interface IGitHubUsersClient
{
    /// <summary>
    /// Retrieves the public GitHub profile for the supplied login.
    /// </summary>
    /// <param name="login">The GitHub account login (case-insensitive).</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A task that resolves to the <see cref="GitHubUser"/> for <paramref name="login"/>,
    /// or <see langword="null"/> when GitHub returns HTTP 404 for the login.
    /// </returns>
    /// <exception cref="GitHubApiException">Thrown when the GitHub API responds with a non-success status other than 404.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="login"/> is <see langword="null"/>, empty or white-space.</exception>
    Task<GitHubUser?> GetUserAsync(string login, CancellationToken cancellationToken = default);
}
