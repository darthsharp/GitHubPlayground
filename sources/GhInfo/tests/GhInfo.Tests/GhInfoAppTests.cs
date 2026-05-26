using AwesomeAssertions;
using GhInfo.Cli;
using Microsoft.Extensions.DependencyInjection;

namespace GhInfo.Tests;

[Collection(nameof(GhInfoAppTests))]
public sealed class GhInfoAppTests : IDisposable
{
    private readonly string _tempCacheDirectory;
    private readonly string _previousCacheEnv;
    private readonly string _previousGitHubTokenEnv;

    public GhInfoAppTests()
    {
        _tempCacheDirectory = Path.Combine(Path.GetTempPath(), "gh-info-tests-" + Guid.NewGuid().ToString("N"));

        _previousCacheEnv = Environment.GetEnvironmentVariable("GHINFO_Cache__DatabasePath") ?? string.Empty;
        _previousGitHubTokenEnv = Environment.GetEnvironmentVariable("GHINFO_GitHub__Token") ?? string.Empty;

        Environment.SetEnvironmentVariable(
            "GHINFO_Cache__DatabasePath",
            Path.Combine(_tempCacheDirectory, "cache.db"));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(
            "GHINFO_Cache__DatabasePath",
            string.IsNullOrEmpty(_previousCacheEnv) ? null : _previousCacheEnv);
        Environment.SetEnvironmentVariable(
            "GHINFO_GitHub__Token",
            string.IsNullOrEmpty(_previousGitHubTokenEnv) ? null : _previousGitHubTokenEnv);

        if (Directory.Exists(_tempCacheDirectory))
        {
            try
            {
                Directory.Delete(_tempCacheDirectory, recursive: true);
            }
            catch (IOException)
            {
                // best effort cleanup
            }
        }
    }

    [Fact]
    public async Task RunAsync_WithHelpFlag_BootstrapsHostAndExitsZero()
    {
        // Arrange — see ctor: cache path redirected to a temp directory so no LocalAppData pollution

        // Act — `--help` prints help via Spectre.Console.Cli and exits without invoking the command.
        // This exercises the full Generic Host + DI + Spectre wiring (including the CommandApp registration),
        // which is the exact path that broke when the singleton was registered under CommandApp<T> only.
        var exitCode = await GhInfoApp.RunAsync(new[] { "--help" });

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public void BuildHost_CanActivateUserInfoCommandAndItsDependencyGraph()
    {
        // Arrange — exercise the exact resolution chain that Spectre walks at runtime
        // (UserInfoCommand → IGhInfoService → IGitHubUsersClient + IUserCacheService → DbContext + TimeProvider).
        // A missing registration anywhere in the graph throws InvalidOperationException here,
        // unlike a `--help` invocation which exits before Spectre activates the command type.
        using var host = GhInfoApp.BuildHost(Array.Empty<string>());
        using var scope = host.Services.CreateScope();

        // Act
        Action act = () => scope.ServiceProvider.GetRequiredService<UserInfoCommand>();

        // Assert
        act.Should().NotThrow();
    }
}
