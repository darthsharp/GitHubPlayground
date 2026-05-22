using CreativeCoders.Core;
using Spectre.Console.Cli;

namespace GhInfo.Cli.Infrastructure;

/// <summary>
/// Bridges Spectre.Console.Cli's type registration to an already-built
/// <see cref="IServiceProvider"/> from the generic host. Registration calls are ignored because
/// all services are configured on the host; resolution delegates to the host container.
/// </summary>
/// <param name="provider">The host service provider used to resolve command dependencies.</param>
internal sealed class TypeRegistrar(IServiceProvider provider) : ITypeRegistrar
{
    private readonly IServiceProvider _provider = Ensure.NotNull(provider);

    /// <inheritdoc />
    public ITypeResolver Build()
    {
        return new TypeResolver(_provider);
    }

    /// <inheritdoc />
    public void Register(Type service, Type implementation)
    {
        // Services are registered on the host; nothing to do here.
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation)
    {
        // Services are registered on the host; nothing to do here.
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Services are registered on the host; nothing to do here.
    }
}
