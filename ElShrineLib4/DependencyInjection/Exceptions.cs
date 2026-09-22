namespace ElShrine.DependencyInjection;

public abstract class DependencyInjectionException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public abstract class RegistrationException(string message)
    : DependencyInjectionException(message);

public abstract class ResolutionException(string message, Exception? innerException = null)
    : DependencyInjectionException(message, innerException);

public sealed class AbstractImplementationException(Type serviceType, Type implementationType)
    : RegistrationException($"Cannot register abstract implementation '{implementationType.FullName ?? implementationType.Name}' for service '{serviceType.FullName ?? serviceType.Name}'.")
{
    public Type ServiceType { get; } = serviceType;
    public Type ImplementationType { get; } = implementationType;
}

public sealed class ServiceUnregisteredException(Type serviceType)
    : ResolutionException($"Service type '{serviceType.FullName ?? serviceType.Name}' is not registered.")
{
    public Type ServiceType { get; } = serviceType;
}

public sealed class CircularDependencyException(Type serviceType, IReadOnlyList<Type> path)
    : ResolutionException($"Circular dependency detected while resolving '{DependentName(path)}' which requires '{serviceType.FullName ?? serviceType.Name}'. Resolution path: {string.Join(" -> ", path.Select(t => t.FullName ?? t.Name))}.")
{
    public Type ServiceType { get; } = serviceType;
    public Type DependentType { get; } = path.Count >= 2 ? path[^2] : serviceType;
    public IReadOnlyList<Type> Path { get; } = path;

    private static string DependentName(IReadOnlyList<Type> path)
    {
        var dependent = path.Count >= 2 ? path[^2] : path[0];
        return dependent.FullName ?? dependent.Name;
    }
}

public sealed class InitiateAbstractsException(Type instanceType)
    : ResolutionException($"Cannot instantiate abstract type or interface '{instanceType.FullName ?? instanceType.Name}'.")
{
    public Type InstanceType { get; } = instanceType;
}

public sealed class NoSuitableConstructorException(Type instanceType, IReadOnlyList<Exception> errors)
    : ResolutionException($"No suitable constructor found for type '{instanceType.FullName ?? instanceType.Name}'.", errors.Count > 0 ? errors[0] : null)
{
    public Type InstanceType { get; } = instanceType;
    public IReadOnlyList<Exception> Errors { get; } = errors;
}
