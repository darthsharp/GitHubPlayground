using GhInfo;
using GhInfo.Caching;
using GhInfo.Cli;
using GhInfo.GitHub;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Configuration
    .AddEnvironmentVariables(prefix: "GHINFO_");

builder.Logging.ClearProviders();
builder.Services.AddSerilog((sp, lc) => lc
    .ReadFrom.Configuration(sp.GetRequiredService<IConfiguration>())
    .Enrich.FromLogContext());

builder.Services
    .AddOptions<GitHubOptions>()
    .BindConfiguration(GitHubOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<CacheOptions>()
    .BindConfiguration(CacheOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

builder.Services
    .AddHttpClient<IGitHubUsersClient, GitHubUsersClient>((sp, http) =>
    {
        var options = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;
        http.BaseAddress = new Uri(options.BaseAddress);
        http.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    });

var cacheConnectionString = new SqliteConnectionStringBuilder { DataSource = ResolveCachePath() }.ToString();
builder.Services.AddDbContext<CacheDbContext>(opt => opt.UseSqlite(cacheConnectionString));

builder.Services.AddScoped<IUserCacheService, UserCacheService>();
builder.Services.AddScoped<GhInfoService>();
builder.Services.AddScoped<UserTableRenderer>();
builder.Services.AddScoped<UserInfoCommand>();

using var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();

var initCache = scope.ServiceProvider.GetRequiredService<IUserCacheService>();
await initCache.InitializeAsync().ConfigureAwait(false);

var app = new CommandApp<UserInfoCommand>(new TypeRegistrar(scope.ServiceProvider));
app.Configure(config =>
{
    config.SetApplicationName("gh-info");
});

try
{
    return await app.RunAsync(args).ConfigureAwait(false);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Fatal:[/] {Markup.Escape(ex.Message)}");
    Log.Logger.Fatal(ex, "Unhandled exception");

    return 99;
}

static string ResolveCachePath()
{
    var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var dir = Path.Combine(folder, "gh-info");
    Directory.CreateDirectory(dir);

    return Path.Combine(dir, "cache.db");
}
