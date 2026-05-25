using CreativeCoders.Core;
using GhInfo.GitHub;
using Spectre.Console;

namespace GhInfo.Cli;

/// <summary>
/// Renders a <see cref="GitHubUser"/> as a colored Spectre.Console table.
/// </summary>
public sealed class UserTableRenderer(IAnsiConsole console)
{
    private readonly IAnsiConsole _console = Ensure.NotNull(console);

    /// <summary>
    /// Writes a table describing <paramref name="user"/> to the underlying console.
    /// </summary>
    /// <param name="user">The user to render.</param>
    /// <param name="fromCache">When <see langword="true"/>, marks the output as a cache hit.</param>
    public void Render(GitHubUser user, bool fromCache)
    {
        Ensure.NotNull(user);

        var source = fromCache
            ? "[yellow]cache[/]"
            : "[green]api[/]";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title($"[bold aqua]GitHub user[/] [bold white]{Markup.Escape(user.Login)}[/]  ({source})")
            .AddColumn(new TableColumn("[bold]Field[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        table.AddRow("[grey]Login[/]", $"[bold white]{Markup.Escape(user.Login)}[/]");
        table.AddRow("[grey]Name[/]", FormatOptional(user.Name));
        table.AddRow("[grey]Bio[/]", FormatOptional(user.Bio));
        table.AddRow("[grey]Public repos[/]", $"[green]{user.PublicRepos}[/]");
        table.AddRow("[grey]Followers[/]", $"[cyan]{user.Followers}[/]");
        table.AddRow("[grey]Created at[/]", $"[magenta]{user.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}[/]");

        _console.Write(table);
    }

    private static string FormatOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "[grey italic](none)[/]"
            : Markup.Escape(value);
    }
}
