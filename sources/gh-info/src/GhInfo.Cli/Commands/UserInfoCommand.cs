using System.ComponentModel;
using System.Globalization;
using CreativeCoders.Core;
using GhInfo.Core;
using GhInfo.Core.GitHub;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GhInfo.Cli.Commands;

/// <summary>
/// Spectre.Console command that displays the public GitHub profile for a given user.
/// </summary>
/// <param name="userService">The service used to resolve the user profile.</param>
/// <param name="console">The console used to render output.</param>
/// <param name="logger">The logger for diagnostic output.</param>
internal sealed class UserInfoCommand(
    IGitHubUserService userService,
    IAnsiConsole console,
    ILogger<UserInfoCommand> logger) : AsyncCommand<UserInfoCommand.Settings>
{
    private readonly IGitHubUserService _userService = Ensure.NotNull(userService);
    private readonly IAnsiConsole _console = Ensure.NotNull(console);
    private readonly ILogger<UserInfoCommand> _logger = Ensure.NotNull(logger);

    /// <summary>The command-line settings for the <see cref="UserInfoCommand"/>.</summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>Gets the GitHub login (handle) to look up.</summary>
        [CommandArgument(0, "<username>")]
        [Description("The GitHub login (handle) to look up.")]
        public string Username { get; init; } = string.Empty;

        /// <summary>Gets a value indicating whether the local cache should be bypassed.</summary>
        [CommandOption("--no-cache")]
        [Description("Skip reading from the cache and fetch a fresh copy from the API (the cache is still refreshed).")]
        public bool NoCache { get; init; }
    }

    /// <inheritdoc />
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        Ensure.NotNull(settings);

        try
        {
            var result = await _userService
                .GetUserAsync(settings.Username, useCache: !settings.NoCache, cancellationToken)
                .ConfigureAwait(false);

            RenderUser(result);

            return 0;
        }
        catch (GitHubUserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User {Login} was not found", settings.Username);
            _console.MarkupLineInterpolated($"[red]User '{settings.Username}' was not found on GitHub.[/]");

            return 1;
        }
        catch (GitHubRateLimitException ex)
        {
            _logger.LogWarning(ex, "GitHub rate limit exceeded");
            _console.MarkupLine("[yellow]The GitHub API rate limit has been exceeded. Try again later " +
                "or configure an access token.[/]");

            return 2;
        }
        catch (GitHubException ex)
        {
            _logger.LogError(ex, "GitHub API request failed");
            _console.MarkupLineInterpolated($"[red]GitHub API request failed: {ex.Message}[/]");

            return 3;
        }
    }

    private void RenderUser(UserInfoResult result)
    {
        var user = result.User;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title($"[bold aqua]{Markup.Escape(user.Login)}[/]")
            .AddColumn(new TableColumn("[grey]Field[/]"))
            .AddColumn(new TableColumn("[grey]Value[/]"));

        table.AddRow("[bold]Login[/]", Markup.Escape(user.Login));
        table.AddRow("[bold]Name[/]", Markup.Escape(user.Name ?? "—"));
        table.AddRow("[bold]Bio[/]", Markup.Escape(user.Bio ?? "—"));
        table.AddRow("[bold]Public repos[/]", $"[green]{user.PublicRepos}[/]");
        table.AddRow("[bold]Followers[/]", $"[green]{user.Followers}[/]");
        table.AddRow(
            "[bold]Created[/]",
            user.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        _console.Write(table);

        var source = result.FromCache ? "[grey]local cache[/]" : "[grey]GitHub API[/]";
        _console.MarkupLine($"Source: {source}");
    }
}
