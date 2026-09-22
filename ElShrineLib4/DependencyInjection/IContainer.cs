namespace ElShrine.DependencyInjection;

public interface IContainer : IServiceNode
{
    void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime);

    void Register(Type serviceType, object instance);
}
