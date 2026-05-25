using CreativeCoders.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace GhInfo.Cli;

/// <summary>
/// Bridges Spectre.Console.Cli's <see cref="ITypeRegistrar"/> to an existing
/// <see cref="IServiceProvider"/> from the Generic Host.
/// </summary>
/// <remarks>
/// Spectre's <c>CommandApp</c> registers a few additional types at configuration time
/// (e.g. command implementations and remaining-args). They are kept in a small
/// supplemental container; resolution falls back to the host's <see cref="IServiceProvider"/>
/// for everything else.
/// </remarks>
internal sealed class TypeRegistrar(IServiceProvider hostProvider) : ITypeRegistrar
{
    private readonly IServiceProvider _hostProvider = Ensure.NotNull(hostProvider);
    private readonly ServiceCollection _supplemental = [];

    public ITypeResolver Build() =>
        new TypeResolver(_hostProvider, _supplemental.BuildServiceProvider());

    public void Register(Type service, Type implementation) =>
        _supplemental.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) =>
        _supplemental.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory)
    {
        Ensure.NotNull(factory);
        _supplemental.AddSingleton(service, _ => factory());
    }
}

/// <summary>
/// Resolves types from a supplemental container first, then falls back to the host provider.
/// </summary>
internal sealed class TypeResolver(IServiceProvider hostProvider, ServiceProvider supplemental)
    : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _hostProvider = Ensure.NotNull(hostProvider);
    private readonly ServiceProvider _supplemental = Ensure.NotNull(supplemental);

    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        return _hostProvider.GetService(type) ?? _supplemental.GetService(type);
    }

    public void Dispose() => _supplemental.Dispose();
}
