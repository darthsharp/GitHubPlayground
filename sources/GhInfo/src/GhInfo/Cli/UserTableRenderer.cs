using System.Globalization;
using CreativeCoders.Core;
using GhInfo.GitHub;
using Spectre.Console;

namespace GhInfo.Cli;

/// <summary>
/// Default <see cref="IUserTableRenderer"/> implementation that writes a
/// two-column key/value table to the injected <see cref="IAnsiConsole"/>.
/// </summary>
public sealed class UserTableRenderer(IAnsiConsole console) : IUserTableRenderer
{
    private readonly IAnsiConsole _console = Ensure.NotNull(console);

    /// <inheritdoc />
    public void Render(GitHubUser user)
    {
        Ensure.NotNull(user);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title($"[bold yellow]GitHub user[/] [bold cyan]{Markup.Escape(user.Login)}[/]")
            .AddColumn(new TableColumn("[bold]Field[/]").NoWrap())
            .AddColumn(new TableColumn("[bold]Value[/]"));

        table.AddRow("[green]Login[/]", Markup.Escape(user.Login));
        table.AddRow("[green]Name[/]", FormatOptional(user.Name));
        table.AddRow("[green]Bio[/]", FormatOptional(user.Bio));
        table.AddRow("[green]Public repos[/]", user.PublicRepos.ToString(CultureInfo.InvariantCulture));
        table.AddRow("[green]Followers[/]", user.Followers.ToString(CultureInfo.InvariantCulture));
        table.AddRow("[green]Created at[/]", user.CreatedAt.ToString("u", CultureInfo.InvariantCulture));

        _console.Write(table);
    }

    private static string FormatOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "[grey](not set)[/]"
            : Markup.Escape(value);
    }
}
