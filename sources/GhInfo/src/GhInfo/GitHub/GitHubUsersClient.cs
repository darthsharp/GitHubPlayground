using System.Net;
using System.Net.Http.Json;
using CreativeCoders.Core;
using Microsoft.Extensions.Logging;

namespace GhInfo.GitHub;

/// <summary>
/// Typed HTTP client that talks to the GitHub REST API.
/// </summary>
public sealed class GitHubUsersClient(HttpClient httpClient, ILogger<GitHubUsersClient> logger) : IGitHubUsersClient
{
    private readonly HttpClient _httpClient = Ensure.NotNull(httpClient);
    private readonly ILogger<GitHubUsersClient> _logger = Ensure.NotNull(logger);

    /// <inheritdoc/>
    public async Task<GitHubUser> GetUserAsync(string login, CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        var requestUri = $"users/{Uri.EscapeDataString(login)}";

        _logger.LogInformation("Fetching GitHub user {Login} from {RequestUri}", login, requestUri);

        using var response = await _httpClient
            .GetAsync(requestUri, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new GitHubUserNotFoundException(login);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            throw new GitHubApiException(
                response.StatusCode,
                $"GitHub API request failed with status {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        var user = await response.Content
            .ReadFromJsonAsync<GitHubUser>(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new GitHubApiException(
                response.StatusCode,
                "GitHub API returned an empty response body.");
        }

        return user;
    }
}
