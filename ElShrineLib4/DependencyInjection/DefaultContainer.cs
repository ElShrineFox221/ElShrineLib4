using System.Collections.Concurrent;

namespace ElShrine.DependencyInjection;

public sealed class DefaultContainer : IContainer, IDisposable
{
    public bool AllowUnregistered { get; set; } = false;
    private class DefaultServiceScope(DefaultContainer container) : IServiceScope, IServiceProvider
    {
        private readonly ConcurrentDictionary<Type, object> _cachedServices = [];
        private readonly DefaultContainer _container = container;
        public IServiceProvider ServiceProvider => this;
        
        public object? GetService(Type serviceType)
            => _container.GetServiceInternal(this, serviceType, _cachedServices, true);

        #region Dispose
        private bool disposedValue;
        private void DisposeInternal()
        {
            if (!disposedValue)
            {
                foreach (var entry in _cachedServices)
                    if (entry.Value is IDisposable d)
                        d.Dispose();
                _cachedServices.Clear();
                disposedValue = true;
            }
        }
        ~DefaultServiceScope()
            => DisposeInternal();
        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    private readonly ConcurrentDictionary<Type, ServiceInfo> _serviceRegs = [];
    private readonly ConcurrentDictionary<Type, Type> _typeMap = [];
    private readonly ConcurrentDictionary<Type, object> _singletons = [];
    private readonly ConcurrentDictionary<Type, object> _emptyScopeds = [];
   
    public IServiceScope CreateScope()
    {
        var scope = new DefaultServiceScope(this);

        return scope;
    }

    private object? ResolveContainer(Type type, object? source)
    {
        if (type == typeof(IContainer) || type == typeof(IServiceScopeFactory) || type == typeof(DefaultContainer))
            return this;
        if (type == typeof(IServiceProvider))
            return source ?? this;
        return null;
    }
    private object? GetServiceInternal(object? source, Type type, ConcurrentDictionary<Type, object> scopeds, bool allowScopeds)
    {
        var preResolve = ResolveContainer(type, source);
        if (preResolve is not null)
            return preResolve;
        if (TryGetServiceInfo(type, out var info))
        {
            if (!allowScopeds && info.Lifetime == ServiceLifetime.Scoped)
                throw new InvalidOperationException($"Scoped service {type.Name} cannot be directly resolved with container.");
            return ActivatorUtilities.ServiceConstructorInvoker(type, t => ResolveContainer(t, source), _singletons, scopeds, _serviceRegs);
        }
        if (!AllowUnregistered)
            throw new InvalidOperationException($"Type {type.Name} is not registered.");
        return ActivatorUtilities.InstanceConstructorInvoker(type, _singletons, _typeMap, cacheUnmappeds: false);
    }
    public object? GetService(Type serviceType)
        => GetServiceInternal(this, serviceType, _emptyScopeds, false);

    #region Register
    public bool TryRegister<TService, TImplementation>(ServiceLifetime lifetime)
        where TService : notnull
        where TImplementation : class, TService
    {
        var type = typeof(TService);
        var implType = typeof(TImplementation);
        var info = new ServiceInfo(implType, lifetime);
        var r = false;
        _serviceRegs.GetOrAdd(type, _ =>
        {
            r = true;
            _typeMap.TryAdd(type, implType);
            return info;
        });
        return r;
    }
    public bool TryRegister<TService>(TService instance) where TService : class
    {
        var type = typeof(TService);
        var implType = instance.GetType();
        var info = new ServiceInfo(implType, ServiceLifetime.Singleton);
        var r = false;
        _serviceRegs.GetOrAdd(type, t =>
        {
            r = true;
            _typeMap.TryAdd(type, implType);
            _singletons.AddOrUpdate(t, _ => instance, (_, _) => instance);
            return info;
        });
        return r;
    }

    public bool TryGetServiceInfo(Type serviceType, out ServiceInfo serviceInfo)
        => _serviceRegs.TryGetValue(serviceType, out serviceInfo);
    #endregion

    #region Dispose
    private bool disposedValue;
    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _serviceRegs.Clear();
                _typeMap.Clear();
            }
            foreach (var entry in _singletons)
                if (entry.Value is IDisposable d)
                    d.Dispose();
            _singletons.Clear();
            disposedValue = true;
        }
    }
    ~DefaultContainer()
        => Dispose(disposing: false);
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
