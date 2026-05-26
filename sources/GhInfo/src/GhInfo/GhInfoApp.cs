using System.Net.Http.Headers;
using CreativeCoders.Core;
using GhInfo.Caching;
using GhInfo.Cli;
using GhInfo.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GhInfo;

/// <summary>
/// Bootstraps the <c>gh-info</c> command-line application: builds the
/// <see cref="IHost"/> with Serilog, configuration, dependency injection and
/// the Spectre.Console.Cli command pipeline.
/// </summary>
public static class GhInfoApp
{
    /// <summary>
    /// Builds the host and executes the Spectre.Console command pipeline with
    /// the supplied command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the process.</param>
    /// <returns>The exit code that the process should return.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        using var host = BuildHost(args);

        await InitializeCacheAsync(host).ConfigureAwait(false);

        var app = host.Services.GetRequiredService<ICommandApp>();

        return await app.RunAsync(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Composes the <see cref="IHost"/> used by <see cref="RunAsync"/>: configuration,
    /// Serilog, dependency-injection registrations, and the Spectre.Console.Cli pipeline.
    /// </summary>
    /// <param name="args">The command-line arguments forwarded to the host builder.</param>
    /// <returns>The composed but not-yet-started host.</returns>
    internal static IHost BuildHost(string[] args)
    {
        Ensure.NotNull(args);

        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "GHINFO_");

        builder.Services.AddSerilog((services, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services));

        ConfigureOptions(builder);
        ConfigureGitHubClient(builder);
        ConfigureCache(builder);
        ConfigureCli(builder);

        builder.Services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);

        return builder.Build();
    }

    private static void ConfigureOptions(HostApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<GitHubOptions>()
            .Bind(builder.Configuration.GetSection(GitHubOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddOptions<CacheOptions>()
            .Bind(builder.Configuration.GetSection(CacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void ConfigureGitHubClient(HostApplicationBuilder builder)
    {
        builder.Services
            .AddHttpClient<IGitHubUsersClient, GitHubUsersClient>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<GitHubOptions>>().Value;

                httpClient.BaseAddress = new Uri(options.BaseAddress);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

                if (!string.IsNullOrWhiteSpace(options.Token))
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", options.Token);
                }
            });
    }

    private static void ConfigureCache(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddDbContext<CacheDbContext>((serviceProvider, dbOptions) =>
        {
            var cacheOptions = serviceProvider.GetRequiredService<IOptions<CacheOptions>>().Value;
            dbOptions.UseSqlite($"Data Source={cacheOptions.GetDatabasePath()}");
        });

        builder.Services.AddScoped<IUserCacheService, UserCacheService>();
    }

    private static void ConfigureCli(HostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IGhInfoService, GhInfoService>();
        builder.Services.AddSingleton<IUserTableRenderer, UserTableRenderer>();
        builder.Services.AddTransient<UserInfoCommand>();

        builder.Services.AddSingleton<ICommandApp>(serviceProvider =>
        {
            var registrar = new TypeRegistrar(serviceProvider);
            var commandApp = new CommandApp<UserInfoCommand>(registrar);
            commandApp.Configure(config =>
            {
                config.SetApplicationName("gh-info");
                config.PropagateExceptions();
            });

            return commandApp;
        });
    }

    private static async Task InitializeCacheAsync(IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CacheDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CacheDbContext>>();

        var cacheOptions = scope.ServiceProvider.GetRequiredService<IOptions<CacheOptions>>().Value;
        var databasePath = cacheOptions.GetDatabasePath();
        var directory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);

        logger.LogDebug("Cache database initialized at {DatabasePath}", databasePath);
    }
}
