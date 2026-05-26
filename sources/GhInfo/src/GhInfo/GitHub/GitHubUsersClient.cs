using System.Net;
using System.Net.Http.Json;
using CreativeCoders.Core;
using Microsoft.Extensions.Logging;

namespace GhInfo.GitHub;

/// <summary>
/// Typed HTTP client that calls the GitHub <c>/users/{username}</c> REST endpoint.
/// </summary>
public sealed class GitHubUsersClient(HttpClient httpClient, ILogger<GitHubUsersClient> logger) : IGitHubUsersClient
{
    private readonly HttpClient _httpClient = Ensure.NotNull(httpClient);
    private readonly ILogger<GitHubUsersClient> _logger = Ensure.NotNull(logger);

    /// <inheritdoc />
    public async Task<GitHubUser?> GetUserAsync(string login, CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        var requestUri = $"users/{Uri.EscapeDataString(login)}";

        _logger.LogDebug("Requesting GitHub user {Login} from {RequestUri}", login, requestUri);

        using var response = await _httpClient
            .GetAsync(requestUri, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("GitHub user {Login} not found", login);

            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            throw new GitHubApiException(
                response.StatusCode,
                body,
                $"GitHub API request for user '{login}' failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        var user = await response.Content
            .ReadFromJsonAsync<GitHubUser>(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new GitHubApiException(
                response.StatusCode,
                responseBody: null,
                $"GitHub API returned an empty body for user '{login}'.");
        }

        return user;
    }
}
