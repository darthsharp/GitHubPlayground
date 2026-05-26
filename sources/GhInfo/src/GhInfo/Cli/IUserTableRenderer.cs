using GhInfo.GitHub;

namespace GhInfo.Cli;

/// <summary>
/// Renders a <see cref="GitHubUser"/> as a colored Spectre.Console table.
/// </summary>
public interface IUserTableRenderer
{
    /// <summary>
    /// Writes a formatted, color-highlighted table representation of the
    /// supplied user to the underlying console.
    /// </summary>
    /// <param name="user">The user snapshot to render.</param>
    void Render(GitHubUser user);
}
