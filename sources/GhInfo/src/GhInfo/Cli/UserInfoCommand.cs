using System.ComponentModel;
using CreativeCoders.Core;
using GhInfo.GitHub;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GhInfo.Cli;

/// <summary>
/// Spectre.Console.Cli command that resolves a single GitHub user and renders the result.
/// </summary>
public class UserInfoCommand(
    IGhInfoService service,
    IUserTableRenderer renderer,
    IAnsiConsole console,
    ILogger<UserInfoCommand> logger) : AsyncCommand<UserInfoCommand.Settings>
{
    private readonly IGhInfoService _service = Ensure.NotNull(service);
    private readonly IUserTableRenderer _renderer = Ensure.NotNull(renderer);
    private readonly IAnsiConsole _console = Ensure.NotNull(console);
    private readonly ILogger<UserInfoCommand> _logger = Ensure.NotNull(logger);

    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        Ensure.NotNull(settings);

        try
        {
            var result = await _service
                .GetUserAsync(settings.Username, useCache: !settings.NoCache, cancellationToken)
                .ConfigureAwait(false);

            _renderer.Render(result.User, result.FromCache);

            return 0;
        }
        catch (GitHubUserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Login} not found", ex.Login);
            _console.MarkupLine($"[red]User '{Markup.Escape(ex.Login)}' was not found on GitHub.[/]");

            return 1;
        }
        catch (GitHubApiException ex)
        {
            _logger.LogError(ex, "GitHub API call failed");
            _console.MarkupLine($"[red]GitHub API error ({(int)ex.StatusCode}): {Markup.Escape(ex.Message)}[/]");

            return 2;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network failure calling GitHub");
            _console.MarkupLine($"[red]Network error: {Markup.Escape(ex.Message)}[/]");

            return 3;
        }
    }

    /// <summary>
    /// Command-line settings for <see cref="UserInfoCommand"/>.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the GitHub login to look up.
        /// </summary>
        [CommandArgument(0, "<username>")]
        [Description("The GitHub login (handle) to look up.")]
        public required string Username { get; init; }

        /// <summary>
        /// Gets a value indicating whether the local cache should be bypassed.
        /// </summary>
        [CommandOption("--no-cache")]
        [Description("Skip the local cache read and always call the GitHub API (the fresh response is still written back to the cache).")]
        [DefaultValue(false)]
        public bool NoCache { get; init; }
    }
}
