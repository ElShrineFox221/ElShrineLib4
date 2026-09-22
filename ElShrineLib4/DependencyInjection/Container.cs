using System.Reflection;

namespace ElShrine.DependencyInjection;

public sealed class Container : IContainer
{
    private readonly struct ServiceInfo(Type implementType, ServiceLifetime lifetime)
    {
        public readonly Type ImplementType = implementType;
        public readonly ServiceLifetime Lifetime = lifetime;
    }

    private static readonly NullabilityInfoContext _nullability = new();

    private readonly object _sync;
    private readonly Container? _parent;
    private readonly Dictionary<Type, ServiceInfo> _registrations = [];
    private readonly Dictionary<Type, object> _instances = [];
    private readonly List<Container> _children = [];
    private readonly UnregisteredPolicy _unregisteredPolicy;
    private bool _disposed;

    private Container(Container? parent, object sync, ScopeOptions options)
    {
        _parent = parent;
        _sync = sync;
        _unregisteredPolicy = options.Unregistered == UnregisteredPolicy.Inherit
            ? parent?._unregisteredPolicy ?? UnregisteredPolicy.Reject
            : options.Unregistered;
    }

    public static IContainer CreateRoot(ScopeOptions options = default)
        => new Container(null, new object(), options);

    public IServiceNode? Parent => _parent;

    public IContainer CreateScope(ScopeOptions options = default)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            var child = new Container(this, _sync, options);
            _children.Add(child);
            return child;
        }
    }

    public void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        lock (_sync)
        {
            ThrowIfDisposed();
            if (implementationType.IsAbstract)
                throw new AbstractImplementationException(serviceType, implementationType);
            _registrations[serviceType] = new ServiceInfo(implementationType, lifetime);
        }
    }

    public void Register(Type serviceType, object instance)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(instance);
        if (!serviceType.IsInstanceOfType(instance))
            throw new ArgumentException(
                $"Instance of type '{instance.GetType().FullName}' is not assignable to service type '{serviceType.FullName}'.",
                nameof(instance));
        lock (_sync)
        {
            ThrowIfDisposed();
            _registrations[serviceType] = new ServiceInfo(instance.GetType(), ServiceLifetime.Singleton);
            _instances[serviceType] = instance;
        }
    }

    public IServiceNode? GetRegistrationOwner(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        lock (_sync)
        {
            ThrowIfDisposed();
            return FindOwner(this, serviceType);
        }
    }

    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        lock (_sync)
        {
            ThrowIfDisposed();
            var context = new ResolutionContext(this);
            return Resolve(serviceType, context, allowNullable: true);
        }
    }

    public void Dispose()
    {
        List<Container> children;
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            children = [.. _children];
            _children.Clear();
        }
        foreach (var child in children)
            child.Dispose();
        lock (_sync)
        {
            foreach (var instance in _instances.Values)
                if (instance is IDisposable disposable)
                    disposable.Dispose();
            _instances.Clear();
            _registrations.Clear();
        }
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);

    private static Container? FindOwner(Container from, Type serviceType)
    {
        for (var node = from; node is not null; node = node._parent)
            if (node._registrations.ContainsKey(serviceType))
                return node;
        return null;
    }

    private static bool IsIntrinsic(Type type)
        => type == typeof(IServiceProvider)
        || type == typeof(IServiceNode)
        || type == typeof(IContainer)
        || type == typeof(Container);

    private object? Resolve(Type serviceType, ResolutionContext context, bool allowNullable)
    {
        if (IsIntrinsic(serviceType))
            return context.Origin;

        if (context.TemporaryCache.TryGetValue(serviceType, out var temporary))
            return temporary;

        var owner = FindOwner(context.Origin, serviceType);
        if (owner is null)
            return ResolveUnregistered(serviceType, context, allowNullable);

        var info = owner._registrations[serviceType];
        return info.Lifetime switch
        {
            ServiceLifetime.Singleton => ResolveSingleton(serviceType, info, owner, context),
            ServiceLifetime.Scoped => ResolveScoped(serviceType, info, context),
            _ => ResolveTransient(serviceType, info, context)
        };
    }

    private object? ResolveUnregistered(Type serviceType, ResolutionContext context, bool allowNullable)
    {
        if (context.Origin._unregisteredPolicy != UnregisteredPolicy.Construct)
            return allowNullable ? null : throw new ServiceUnregisteredException(serviceType);

        if (serviceType.IsInterface || serviceType.IsAbstract)
            throw new InitiateAbstractsException(serviceType);

        return ConstructWithStack(serviceType, serviceType, context, cacheTemporary: true);
    }

    private object ResolveSingleton(Type serviceType, ServiceInfo info, Container owner, ResolutionContext context)
    {
        if (owner._instances.TryGetValue(serviceType, out var existing))
            return existing;

        var previousOrigin = context.Origin;
        context.Origin = owner;
        try
        {
            var instance = ConstructWithStack(serviceType, info.ImplementType, context, cacheTemporary: false);
            owner._instances[serviceType] = instance;
            return instance;
        }
        finally
        {
            context.Origin = previousOrigin;
        }
    }

    private object ResolveScoped(Type serviceType, ServiceInfo info, ResolutionContext context)
    {
        var origin = context.Origin;
        if (origin._instances.TryGetValue(serviceType, out var existing))
            return existing;

        var instance = ConstructWithStack(serviceType, info.ImplementType, context, cacheTemporary: false);
        origin._instances[serviceType] = instance;
        return instance;
    }

    private object ResolveTransient(Type serviceType, ServiceInfo info, ResolutionContext context)
        => ConstructWithStack(serviceType, info.ImplementType, context, cacheTemporary: true);

    private object ConstructWithStack(Type serviceType, Type implementType, ResolutionContext context, bool cacheTemporary)
    {
        if (context.ResolvingStack.Contains(serviceType))
            throw new CircularDependencyException(serviceType, context.BuildCyclePath(serviceType));

        context.ResolvingStack.Push(serviceType);
        try
        {
            var instance = CreateInstance(implementType, context);
            if (cacheTemporary)
                context.TemporaryCache[serviceType] = instance;
            return instance;
        }
        finally
        {
            context.ResolvingStack.Pop();
        }
    }

    private object CreateInstance(Type instanceType, ResolutionContext context)
    {
        if (instanceType.IsInterface || instanceType.IsAbstract)
            throw new InitiateAbstractsException(instanceType);

        var constructors = instanceType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length);
        var errors = new List<Exception>();
        foreach (var constructor in constructors)
        {
            try
            {
                return InvokeConstructor(constructor, context);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }
        }
        throw new NoSuitableConstructorException(instanceType, errors);
    }

    private object InvokeConstructor(ConstructorInfo constructor, ResolutionContext context)
    {
        var parameters = constructor.GetParameters();
        var args = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.HasDefaultValue)
            {
                args[i] = parameter.DefaultValue;
                continue;
            }
            var parameterType = parameter.ParameterType;
            var nullable = _nullability.Create(parameter).ReadState == NullabilityState.Nullable;
            if (parameterType == typeof(string))
            {
                args[i] = nullable ? null : string.Empty;
                continue;
            }
            if (parameterType.IsValueType)
            {
                args[i] = nullable ? null : Activator.CreateInstance(parameterType);
                continue;
            }
            args[i] = Resolve(parameterType, context, nullable);
        }
        return constructor.Invoke(args);
    }
}
