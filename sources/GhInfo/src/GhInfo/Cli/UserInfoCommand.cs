using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CreativeCoders.Core;
using GhInfo.GitHub;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GhInfo.Cli;

/// <summary>
/// Spectre.Console.Cli command that displays public information for a single
/// GitHub user.
/// </summary>
[UsedImplicitly]
public sealed class UserInfoCommand(
    IGhInfoService ghInfoService,
    IUserTableRenderer tableRenderer,
    IAnsiConsole console,
    ILogger<UserInfoCommand> logger) : AsyncCommand<UserInfoCommand.Settings>
{
    private const int ExitSuccess = 0;
    private const int ExitNotFound = 1;
    private const int ExitApiError = 2;

    private readonly IGhInfoService _ghInfoService = Ensure.NotNull(ghInfoService);
    private readonly IUserTableRenderer _tableRenderer = Ensure.NotNull(tableRenderer);
    private readonly IAnsiConsole _console = Ensure.NotNull(console);
    private readonly ILogger<UserInfoCommand> _logger = Ensure.NotNull(logger);

    /// <inheritdoc />
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        Ensure.NotNull(context);
        Ensure.NotNull(settings);

        try
        {
            var user = await _ghInfoService
                .GetUserAsync(settings.Username, useCache: !settings.NoCache, cancellationToken)
                .ConfigureAwait(false);

            if (user is null)
            {
                _console.MarkupLine($"[red]No GitHub user found for[/] [bold]{Markup.Escape(settings.Username)}[/].");

                return ExitNotFound;
            }

            _tableRenderer.Render(user);

            return ExitSuccess;
        }
        catch (GitHubApiException ex)
        {
            _logger.LogError(ex, "GitHub API call failed for {Login}", settings.Username);
            _console.MarkupLine($"[red]GitHub API error:[/] {Markup.Escape(ex.Message)}");

            return ExitApiError;
        }
    }

    /// <summary>
    /// Command-line settings for <see cref="UserInfoCommand"/>.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1034:Nested types should not be visible",
        Justification = "Spectre.Console.Cli idiomatically nests command settings types inside their owning command.")]
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the GitHub login of the user to look up.
        /// </summary>
        /// <value>A non-empty GitHub account login.</value>
        [CommandArgument(0, "<USERNAME>")]
        [Description("GitHub login of the user to look up.")]
        public required string Username { get; init; }

        /// <summary>
        /// Gets a value indicating whether the local cache should be bypassed.
        /// </summary>
        /// <value><see langword="true"/> to bypass the cache; otherwise <see langword="false"/>.</value>
        [CommandOption("--no-cache")]
        [Description("Bypass the local cache and force a fresh GitHub API call.")]
        public bool NoCache { get; init; }
    }
}
