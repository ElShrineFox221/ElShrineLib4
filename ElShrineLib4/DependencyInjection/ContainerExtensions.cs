namespace ElShrine.DependencyInjection;

public static class ContainerExtensions
{
    #region Register
    public static void Register<TService, TImplementation>(this IContainer container, ServiceLifetime lifetime)
        where TService : notnull
        where TImplementation : class, TService
        => container.Register(typeof(TService), typeof(TImplementation), lifetime);

    public static void Register<TService>(this IContainer container, TService instance)
        where TService : class
        => container.Register(typeof(TService), instance);

    public static void Register<TService>(this IContainer container, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
        => container.Register(typeof(TService), lifetime);
    #endregion

    #region Resolve
    public static T? GetService<T>(this IServiceNode node)
        => node.GetService(typeof(T)) is T service ? service : default;

    public static T GetRequiredService<T>(this IServiceNode node)
        where T : notnull
    {
        if (node.GetService(typeof(T)) is T service)
            return service;
        throw new ServiceUnregisteredException(typeof(T));
    }

    public static object GetRequiredService(this IServiceNode node, Type serviceType)
        => node.GetService(serviceType) ?? throw new ServiceUnregisteredException(serviceType);
    #endregion

    #region Registration owner
    public static IServiceNode? GetRegistrationOwner<TService>(this IServiceNode node)
        => node.GetRegistrationOwner(typeof(TService));
    #endregion
}
