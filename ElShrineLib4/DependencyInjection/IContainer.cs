namespace ElShrine.DependencyInjection;

public interface IContainer : IServiceProvider, IServiceScopeFactory
{
    bool TryGetServiceInfo(Type serviceType, out ServiceInfo serviceInfo);
    bool TryGetServiceInfo<TService>(out ServiceInfo serviceInfo)
        where TService : notnull
        => TryGetServiceInfo(typeof(TService), out serviceInfo);

    bool TryRegister<TService, TImplementation>(ServiceLifetime lifetime)
        where TService : notnull
        where TImplementation : class, TService;

    bool TryRegister<TService>(TService instance)
        where TService : class;
}
