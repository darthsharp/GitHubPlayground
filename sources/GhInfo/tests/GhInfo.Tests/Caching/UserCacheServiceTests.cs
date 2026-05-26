using AwesomeAssertions;
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
    private CacheDbContext _dbContext = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();

        _dbContext = CreateDbContext();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetAsync_WhenEntryMissing_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut(out _);

        // Act
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenEntryFresh_ReturnsCachedUser()
    {
        // Arrange
        var sut = CreateSut(out var timeProvider);
        var user = CreateUser("octocat");
        await sut.SetAsync(user);

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        // Act
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be("octocat");
        result.PublicRepos.Should().Be(8);
    }

    [Fact]
    public async Task GetAsync_WhenEntryExceedsTtl_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut(out var timeProvider, durationMinutes: 15);
        await sut.SetAsync(CreateUser("octocat"));

        timeProvider.Advance(TimeSpan.FromMinutes(16));

        // Act
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_NormalizesLoginCase()
    {
        // Arrange
        var sut = CreateSut(out _);
        await sut.SetAsync(CreateUser("Octocat"));

        // Act
        var result = await sut.GetAsync("OCTOCAT");

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be("octocat");
    }

    [Fact]
    public async Task SetAsync_OverwritesPreviousEntryAndRefreshesTimestamp()
    {
        // Arrange
        var sut = CreateSut(out var timeProvider, durationMinutes: 15);
        await sut.SetAsync(CreateUser("octocat", publicRepos: 1));

        timeProvider.Advance(TimeSpan.FromMinutes(14));

        // Act
        await sut.SetAsync(CreateUser("octocat", publicRepos: 42));

        timeProvider.Advance(TimeSpan.FromMinutes(10));
        var result = await sut.GetAsync("octocat");

        // Assert — second SetAsync replaces the entry AND resets the TTL window
        result.Should().NotBeNull();
        result!.PublicRepos.Should().Be(42);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_WithBlankLogin_Throws(string login)
    {
        // Arrange
        var sut = CreateSut(out _);

        // Act
        Func<Task> act = () => sut.GetAsync(login);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithNullUser_Throws()
    {
        // Arrange
        var sut = CreateSut(out _);

        // Act
        Func<Task> act = () => sut.SetAsync(user: null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private CacheDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CacheDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new CacheDbContext(options);
    }

    private UserCacheService CreateSut(out FakeTimeProvider timeProvider, int durationMinutes = 15)
    {
        timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-26T12:00:00Z"));
        var options = Options.Create(new CacheOptions { DurationMinutes = durationMinutes });

        return new UserCacheService(_dbContext, options, timeProvider, NullLogger<UserCacheService>.Instance);
    }

    private static GitHubUser CreateUser(string login, int publicRepos = 8)
    {
        return new GitHubUser(
            login,
            Name: "The Octocat",
            Bio: "GitHub mascot",
            PublicRepos: publicRepos,
            Followers: 1234,
            CreatedAt: DateTimeOffset.Parse("2008-01-14T04:33:35Z"));
    }
}
