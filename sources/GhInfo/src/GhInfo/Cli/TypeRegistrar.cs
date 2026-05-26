using CreativeCoders.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace GhInfo.Cli;

/// <summary>
/// Bridges Spectre.Console.Cli's <see cref="ITypeRegistrar"/> contract onto the
/// host's <see cref="IServiceProvider"/>: application services are resolved
/// from the host, while Spectre's own runtime registrations are kept in a
/// secondary container.
/// </summary>
public sealed class TypeRegistrar(IServiceProvider rootProvider) : ITypeRegistrar
{
    private readonly IServiceProvider _rootProvider = Ensure.NotNull(rootProvider);
    private readonly ServiceCollection _localServices = new();

    /// <inheritdoc />
    public ITypeResolver Build()
    {
        return new TypeResolver(_rootProvider.CreateScope(), _localServices.BuildServiceProvider());
    }

    /// <inheritdoc />
    public void Register(Type service, Type implementation)
    {
        Ensure.NotNull(service);
        Ensure.NotNull(implementation);

        _localServices.AddTransient(service, implementation);
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation)
    {
        Ensure.NotNull(service);
        Ensure.NotNull(implementation);

        _localServices.AddSingleton(service, implementation);
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        Ensure.NotNull(service);
        Ensure.NotNull(factory);

        _localServices.AddSingleton(service, _ => factory());
    }
}
