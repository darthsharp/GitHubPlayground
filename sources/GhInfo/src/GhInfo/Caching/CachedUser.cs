using System.ComponentModel.DataAnnotations;

namespace GhInfo.Caching;

/// <summary>
/// EF Core entity representing a single cached GitHub user lookup.
/// </summary>
public sealed class CachedUser
{
    /// <summary>
    /// Gets or sets the lowercase GitHub login (primary key).
    /// </summary>
    [Key]
    [MaxLength(64)]
    public required string Login { get; set; }

    /// <summary>
    /// Gets or sets the user's display name, or <see langword="null"/> if not set.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the user's profile biography, or <see langword="null"/> if not set.
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the number of public repositories owned by the user.
    /// </summary>
    public int PublicRepos { get; set; }

    /// <summary>
    /// Gets or sets the number of followers of the user.
    /// </summary>
    public int Followers { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which this row was cached.
    /// </summary>
    public DateTimeOffset CachedAt { get; set; }
}
