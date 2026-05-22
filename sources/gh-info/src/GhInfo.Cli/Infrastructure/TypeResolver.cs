using CreativeCoders.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace GhInfo.Cli.Infrastructure;

/// <summary>
/// Resolves Spectre.Console.Cli types from the generic host service provider, falling back to
/// activation for concrete types (such as commands) that are not explicitly registered.
/// </summary>
/// <param name="provider">The host service provider used for resolution.</param>
internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
    private readonly IServiceProvider _provider = Ensure.NotNull(provider);

    /// <inheritdoc />
    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        var service = _provider.GetService(type);
        if (service is not null)
        {
            return service;
        }

        return type.IsAbstract || type.IsInterface
            ? null
            : ActivatorUtilities.CreateInstance(_provider, type);
    }
}
