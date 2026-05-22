using GhInfo.Core.Models;

namespace GhInfo.Core;

/// <summary>
/// The result of a user lookup, indicating both the profile and whether it was served from cache.
/// </summary>
/// <param name="User">The resolved GitHub user profile.</param>
/// <param name="FromCache"><see langword="true"/> if the profile came from the local cache; otherwise <see langword="false"/>.</param>
public sealed record UserInfoResult(GitHubUser User, bool FromCache);
