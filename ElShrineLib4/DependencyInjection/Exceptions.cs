namespace ElShrine.DependencyInjection;

public sealed class ServiceUnregisteredException(Type serviceType) : 
    Exception($"Type {serviceType.Name} is not registered.")
{
    public readonly string ServiceFullName = serviceType.FullName ?? serviceType.Name;
}
public sealed class CircularDependencyException(Type type) : 
    Exception($"Circular dependency detected for type '{type.Name}'.")
{
    public readonly string TypeFullName = type.FullName ?? type.Name;
}
public sealed class InitiateAbstractsException(Type instanceType) : 
    Exception($"Cannot instantiate abstract type or interface '{instanceType.Name}'.") 
{
    public readonly string TypeFullName = instanceType.FullName ?? instanceType.Name;
}
public sealed class NoSuitableConstructorException(Type instanceType, List<Exception> innerExceptions) : 
    AggregateException(message: $"No suitable constructor found for type '{instanceType.Name}'.", innerExceptions)
{
    public readonly string TypeFullName = instanceType.FullName ?? instanceType.Name;
}
public sealed class ResolveScopedServiceInGlobalContainerException(Type serviceType) : 
    Exception($"Scoped service {serviceType.Name} cannot be directly resolved with container.")
{
    public readonly string ServiceFullName = serviceType.FullName ?? serviceType.Name;
}
