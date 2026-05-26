using CreativeCoders.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace GhInfo.Cli;

/// <summary>
/// Spectre.Console.Cli type resolver that consults Spectre's own runtime
/// container first and falls back to the application's scoped host
/// <see cref="IServiceProvider"/>.
/// </summary>
internal sealed class TypeResolver(IServiceScope hostScope, IServiceProvider localProvider) : ITypeResolver, IDisposable
{
    private readonly IServiceScope _hostScope = Ensure.NotNull(hostScope);
    private readonly IServiceProvider _localProvider = Ensure.NotNull(localProvider);

    /// <inheritdoc />
    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        return _hostScope.ServiceProvider.GetService(type) ?? _localProvider.GetService(type);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _hostScope.Dispose();
        (_localProvider as IDisposable)?.Dispose();
    }
}
