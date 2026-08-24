namespace ElShrine.DependencyInjection;

public readonly struct ServiceInfo(Type implementType, ServiceLifetime lifetime)
{
    public readonly Type ImplementType = implementType;
    public readonly ServiceLifetime Lifetime = lifetime;
}
