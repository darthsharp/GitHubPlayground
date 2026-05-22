using AwesomeAssertions;
using GhInfo.Core.Caching;
using GhInfo.Core.Models;
using GhInfo.Core.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GhInfo.Core.Tests.Caching;

public class UserCacheServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 22, 12, 0, 0, TimeSpan.Zero);

    private static UserCacheService CreateSut(
        CacheDbContext context,
        TimeProvider timeProvider,
        TimeSpan? expiration = null)
    {
        var options = Options.Create(new CacheOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(15),
        });

        return new UserCacheService(
            context,
            options,
            timeProvider,
            NullLogger<UserCacheService>.Instance);
    }

    private static GitHubUser CreateUser(string login = "octocat", int followers = 100)
    {
        return new GitHubUser
        {
            Login = login,
            Name = "The Octocat",
            Bio = "Hi",
            PublicRepos = 8,
            Followers = followers,
            CreatedAt = new DateTimeOffset(2011, 1, 25, 0, 0, 0, TimeSpan.Zero),
        };
    }

    [Fact]
    public async Task GetAsync_WhenNoEntryExists_ReturnsNull()
    {
        // Arrange
        using var fixture = new SqliteCacheFixture();
        var sut = CreateSut(fixture.Context, new TestTimeProvider(Now));

        // Act
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsCachedUser()
    {
        // Arrange
        using var fixture = new SqliteCacheFixture();
        var sut = CreateSut(fixture.Context, new TestTimeProvider(Now));
        var user = CreateUser();

        // Act
        await sut.SetAsync(user);
        var result = await sut.GetAsync(user.Login);

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be(user.Login);
        result.Name.Should().Be(user.Name);
        result.PublicRepos.Should().Be(user.PublicRepos);
        result.Followers.Should().Be(user.Followers);
        result.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public async Task GetAsync_WhenEntryIsWithinExpiration_ReturnsUser()
    {
        // Arrange
        using var fixture = new SqliteCacheFixture();
        var clock = new TestTimeProvider(Now);
        var sut = CreateSut(fixture.Context, clock, TimeSpan.FromMinutes(15));
        await sut.SetAsync(CreateUser());

        // Act
        clock.Advance(TimeSpan.FromMinutes(14));
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_WhenEntryIsOlderThanExpiration_ReturnsNull()
    {
        // Arrange
        using var fixture = new SqliteCacheFixture();
        var clock = new TestTimeProvider(Now);
        var sut = CreateSut(fixture.Context, clock, TimeSpan.FromMinutes(15));
        await sut.SetAsync(CreateUser());

        // Act
        clock.Advance(TimeSpan.FromMinutes(16));
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_AtExactExpirationBoundary_ReturnsUser()
    {
        // Arrange
        using var fixture = new SqliteCacheFixture();
        var clock = new TestTimeProvider(Now);
        var sut = CreateSut(fixture.Context, clock, TimeSpan.FromMinutes(15));
        await sut.SetAsync(CreateUser());

        // Act
        clock.Advance(TimeSpan.FromMinutes(15));
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SetAsync_WhenEntryAlreadyExists_UpdatesInPlace()
    {
        // Arrange
        using var fixture = new SqliteCacheFixture();
        var clock = new TestTimeProvider(Now);
        var sut = CreateSut(fixture.Context, clock);
        await sut.SetAsync(CreateUser(followers: 100));

        // Act
        clock.Advance(TimeSpan.FromMinutes(1));
        await sut.SetAsync(CreateUser(followers: 250));
        var result = await sut.GetAsync("octocat");

        // Assert
        result!.Followers.Should().Be(250);
        var count = await fixture.Context.Users.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_WhenLoginCasingDiffers_ReturnsUser()
    {
        // Arrange
        using var fixture = new SqliteCacheFixture();
        var sut = CreateSut(fixture.Context, new TestTimeProvider(Now));
        await sut.SetAsync(CreateUser("OctoCat"));

        // Act
        var result = await sut.GetAsync("octocat");

        // Assert
        result.Should().NotBeNull();
        result!.Login.Should().Be("OctoCat");
    }
}
