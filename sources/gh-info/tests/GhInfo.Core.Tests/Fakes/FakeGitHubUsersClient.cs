using GhInfo.Core.GitHub;
using GhInfo.Core.Models;

namespace GhInfo.Core.Tests.Fakes;

/// <summary>
/// Hand-written fake of <see cref="IGitHubUsersClient"/> that serves preconfigured users, records
/// the logins it was asked for, and can be made to throw a configured exception.
/// </summary>
internal sealed class FakeGitHubUsersClient : IGitHubUsersClient
{
    private readonly Dictionary<string, GitHubUser> _users = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the number of times <see cref="GetUserAsync"/> was called.</summary>
    public int CallCount { get; private set; }

    /// <summary>Gets the logins requested via <see cref="GetUserAsync"/>, in call order.</summary>
    public List<string> RequestedLogins { get; } = [];

    /// <summary>Gets or sets an exception to throw on the next call, instead of returning a user.</summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>Registers a user that the fake will return for its login.</summary>
    /// <param name="user">The user to serve.</param>
    public void AddUser(GitHubUser user)
    {
        _users[user.Login] = user;
    }

    /// <inheritdoc />
    public Task<GitHubUser> GetUserAsync(string login, CancellationToken cancellationToken = default)
    {
        CallCount++;
        RequestedLogins.Add(login);

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return _users.TryGetValue(login, out var user)
            ? Task.FromResult(user)
            : throw new GitHubUserNotFoundException(login);
    }
}
