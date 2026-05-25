using GhInfo.GitHub;

namespace GhInfo.Cli;

/// <summary>
/// Renders a <see cref="GitHubUser"/> to a console surface.
/// </summary>
public interface IUserTableRenderer
{
    /// <summary>
    /// Writes a table describing <paramref name="user"/> to the underlying console.
    /// </summary>
    /// <param name="user">The user to render.</param>
    /// <param name="fromCache">When <see langword="true"/>, marks the output as a cache hit.</param>
    void Render(GitHubUser user, bool fromCache);
}
