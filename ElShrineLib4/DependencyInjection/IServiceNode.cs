namespace ElShrine.DependencyInjection;

public interface IServiceNode : IServiceProvider, IDisposable
{
    IServiceNode? Parent { get; }

    IContainer CreateScope(ScopeOptions options = default);

    IServiceNode? GetRegistrationOwner(Type serviceType);
}
