namespace ElShrine.DependencyInjection;

public interface IContainer : IServiceNode
{
    void Register<TService, TImplementation>(ServiceLifetime lifetime)
        where TService : notnull
        where TImplementation : class, TService;

    void Register<TService>(TService instance)
        where TService : class;
}
