namespace ElShrine.DependencyInjection;

public static class ContainerExtensions
{
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
}
