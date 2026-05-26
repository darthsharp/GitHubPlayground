using GhInfo.GitHub;

namespace GhInfo.Tests.Fakes;

internal sealed class FakeGitHubUsersClient : IGitHubUsersClient
{
    private readonly Dictionary<string, GitHubUser> _users = new(StringComparer.OrdinalIgnoreCase);

    public int GetUserCallCount { get; private set; }

    public List<string> RequestedLogins { get; } = new();

    public Exception? ExceptionToThrow { get; set; }

    public void AddUser(GitHubUser user)
    {
        _users[user.Login] = user;
    }

    public Task<GitHubUser?> GetUserAsync(string login, CancellationToken cancellationToken = default)
    {
        GetUserCallCount++;
        RequestedLogins.Add(login);

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(_users.TryGetValue(login, out var user) ? user : null);
    }
}
