using GhInfo.Cli.Commands;
using GhInfo.Cli.Infrastructure;
using GhInfo.Core.Caching;
using GhInfo.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

// Build the generic host. CLI arguments are intentionally not passed to the configuration
// builder so that Spectre.Console.Cli is the sole consumer of the command-line arguments.
var builder = Host.CreateApplicationBuilder();

builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddGhInfoCore(builder.Configuration);
builder.Services.AddSingleton(AnsiConsole.Console);
builder.Services.AddTransient<UserInfoCommand>();

using var host = builder.Build();

// A single scope spans the whole command run so scoped services (the DbContext and the user
// service) are resolved correctly rather than from the root provider.
await using var scope = host.Services.CreateAsyncScope();

// Ensure the SQLite cache schema exists before the command runs.
var dbContext = scope.ServiceProvider.GetRequiredService<CacheDbContext>();
await dbContext.Database.EnsureCreatedAsync();

// Keep the cache file from growing unbounded by dropping expired entries on each run.
var cache = scope.ServiceProvider.GetRequiredService<IUserCacheService>();
await cache.PruneExpiredAsync();

try
{
    var app = new CommandApp<UserInfoCommand>(new TypeRegistrar(scope.ServiceProvider));
    app.Configure(config =>
    {
        config.SetApplicationName("gh-info");
        config.SetApplicationVersion("1.0.0");
        config.AddExample("octocat");
        config.AddExample("octocat", "--no-cache");
    });

    return await app.RunAsync(args);
}
finally
{
    await Log.CloseAndFlushAsync();
}
