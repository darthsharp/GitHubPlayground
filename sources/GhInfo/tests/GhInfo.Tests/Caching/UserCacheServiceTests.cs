using AwesomeAssertions;
using FakeItEasy;
using GhInfo.Caching;
using GhInfo.GitHub;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace GhInfo.Tests.Caching;

public sealed class UserCacheServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private CacheDbContext _dbContext = default!;
    private FakeTimeProvider _time = default!;
    private UserCacheService _sut = default!;
    private CacheOptions _options = default!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new CacheDbContext(dbOptions);
        await _dbContext.Database.EnsureCreatedAsync();

        _time = new FakeTimeProvider(startDateTime: new DateTimeOffset(2026, 5, 25, 12, 0, 0, TimeSpan.Zero));
        _options = new CacheOptions { TimeToLiveMinutes = 15 };
        _sut = new UserCacheService(
            _dbContext,
            Options.Create(_options),
            _time,
            NullLogger<UserCacheService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task TryGetAsync_WhenEntryMissing_ReturnsNull()
    {
        // Act
        var result = await _sut.TryGetAsync("octocat");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenTryGetAsync_ReturnsCachedUser()
    {
        // Arrange
        var user = MakeUser("octocat");
        await _sut.SetAsync(user);

        // Act
        var result = await _sut.TryGetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be("octocat");
        result.Name.Should().Be(user.Name);
        result.Followers.Should().Be(user.Followers);
        result.PublicRepos.Should().Be(user.PublicRepos);
        result.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task TryGetAsync_IsCaseInsensitive()
    {
        // Arrange
        await _sut.SetAsync(MakeUser("OctoCat"));

        // Act
        var result = await _sut.TryGetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be("octocat");
    }

    [Fact]
    public async Task TryGetAsync_WhenEntryExpired_ReturnsNull()
    {
        // Arrange
        await _sut.SetAsync(MakeUser("octocat"));
        _time.Advance(TimeSpan.FromMinutes(16));

        // Act
        var result = await _sut.TryGetAsync("octocat");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetAsync_AtExactlyTtl_ReturnsCachedUser()
    {
        // Arrange
        await _sut.SetAsync(MakeUser("octocat"));
        _time.Advance(TimeSpan.FromMinutes(15));

        // Act
        var result = await _sut.TryGetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_TwiceForSameLogin_UpdatesExistingEntry()
    {
        // Arrange
        await _sut.SetAsync(MakeUser("octocat", followers: 10));

        _time.Advance(TimeSpan.FromMinutes(5));
        await _sut.SetAsync(MakeUser("octocat", followers: 42));

        // Act
        var result = await _sut.TryGetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
        result!.Followers.Should().Be(42);
        var rows = await _dbContext.Users.AsNoTracking().CountAsync();
        rows.Should().Be(1);
    }

    [Fact]
    public async Task TryGetAsync_WithNullLogin_Throws()
    {
        // Act
        var act = async () => await _sut.TryGetAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TryGetAsync_WithWhitespaceLogin_Throws()
    {
        // Act
        var act = async () => await _sut.TryGetAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithNullUser_Throws()
    {
        // Act
        var act = async () => await _sut.SetAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InitializeAsync_OnFreshDatabase_CreatesSchema()
    {
        // Arrange — make a fresh, uninitialised context
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var dbOptions = new DbContextOptionsBuilder<CacheDbContext>().UseSqlite(connection).Options;
        await using var ctx = new CacheDbContext(dbOptions);
        var sut = new UserCacheService(ctx, Options.Create(_options), _time, NullLogger<UserCacheService>.Instance);

        // Act
        await sut.InitializeAsync();

        // Assert — schema exists, no rows
        var count = await ctx.Users.CountAsync();
        count.Should().Be(0);
    }

    private static GitHubUser MakeUser(string login, int followers = 1)
    {
        return new GitHubUser
        {
            Login = login,
            Name = "Octo Cat",
            Bio = "I am octocat",
            PublicRepos = 7,
            Followers = followers,
            CreatedAt = new DateTimeOffset(2010, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };
    }
}
