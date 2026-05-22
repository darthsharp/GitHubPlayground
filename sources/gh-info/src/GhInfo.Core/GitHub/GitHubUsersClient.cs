using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CreativeCoders.Core;
using GhInfo.Core.Models;
using Microsoft.Extensions.Logging;

namespace GhInfo.Core.GitHub;

/// <summary>
/// Typed <see cref="HttpClient"/> implementation of <see cref="IGitHubUsersClient"/> that
/// talks to the public GitHub REST API.
/// </summary>
internal sealed class GitHubUsersClient(HttpClient httpClient, ILogger<GitHubUsersClient> logger)
    : IGitHubUsersClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient _httpClient = Ensure.NotNull(httpClient);
    private readonly ILogger<GitHubUsersClient> _logger = Ensure.NotNull(logger);

    /// <inheritdoc />
    public async Task<GitHubUser> GetUserAsync(string login, CancellationToken cancellationToken = default)
    {
        Ensure.IsNotNullOrWhitespace(login);

        _logger.LogDebug("Requesting GitHub user {Login} from the API", login);

        var response = await _httpClient
            .GetAsync($"users/{Uri.EscapeDataString(login)}", cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(login, response, cancellationToken).ConfigureAwait(false);

        var user = await response.Content
            .ReadFromJsonAsync<GitHubUser>(JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new GitHubException("The GitHub API returned an unexpected empty response body.");

        _logger.LogInformation("Fetched GitHub user {Login} from the API", login);

        return user;
    }

    private static async Task EnsureSuccessAsync(
        string login,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        throw response.StatusCode switch
        {
            HttpStatusCode.NotFound => new GitHubUserNotFoundException(login, body),
            HttpStatusCode.TooManyRequests => new GitHubRateLimitException(response.StatusCode, body),
            HttpStatusCode.Forbidden when IsRateLimited(response) =>
                new GitHubRateLimitException(response.StatusCode, body),
            _ => new GitHubException(response.StatusCode, body),
        };
    }

    private static bool IsRateLimited(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("X-RateLimit-Remaining", out var values)
            && values.FirstOrDefault() == "0";
    }
}
