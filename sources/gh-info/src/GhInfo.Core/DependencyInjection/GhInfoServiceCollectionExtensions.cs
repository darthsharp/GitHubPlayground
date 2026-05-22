using System.Net.Http.Headers;
using CreativeCoders.Core;
using GhInfo.Core.Caching;
using GhInfo.Core.GitHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace GhInfo.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering the gh-info core services with a dependency injection container.
/// </summary>
public static class GhInfoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the GitHub users client, the SQLite cache, and the user service, binding options
    /// from the <c>GitHub</c> and <c>Cache</c> configuration sections.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configuration">The configuration the options are bound from.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddGhInfoCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Ensure.NotNull(services);
        Ensure.NotNull(configuration);

        services.Configure<GitHubClientOptions>(configuration.GetSection(GitHubClientOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);

        services
            .AddHttpClient<IGitHubUsersClient, GitHubUsersClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<GitHubClientOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseAddress);
                client.Timeout = options.Timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

                if (!string.IsNullOrWhiteSpace(options.AccessToken))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", options.AccessToken);
                }
            })
            .AddStandardResilienceHandler();

        services.AddDbContext<CacheDbContext>((sp, builder) =>
        {
            var options = sp.GetRequiredService<IOptions<CacheOptions>>().Value;
            builder.UseSqlite($"Data Source={options.DatabasePath}");
        });

        services.AddScoped<IUserCacheService, UserCacheService>();
        services.AddScoped<IGitHubUserService, GitHubUserService>();

        return services;
    }
}
