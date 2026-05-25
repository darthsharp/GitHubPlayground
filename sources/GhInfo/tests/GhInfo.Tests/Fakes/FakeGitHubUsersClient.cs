using System.Collections.Concurrent;
using GhInfo.GitHub;

namespace GhInfo.Tests.Fakes;

/// <summary>
/// In-memory fake for <see cref="IGitHubUsersClient"/> used in unit tests.
/// </summary>
public sealed class FakeGitHubUsersClient : IGitHubUsersClient
{
    private readonly ConcurrentDictionary<string, GitHubUser> _users = new(StringComparer.OrdinalIgnoreCase);

    public int CallCount { get; private set; }

    public void AddUser(GitHubUser user) => _users[user.Login] = user;

    public Task<GitHubUser> GetUserAsync(string login, CancellationToken cancellationToken = default)
    {
        CallCount++;

        if (!_users.TryGetValue(login, out var user))
        {
            throw new GitHubUserNotFoundException(login);
        }

        return Task.FromResult(user);
    }
}
