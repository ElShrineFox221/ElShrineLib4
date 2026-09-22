using ElShrine.DependencyInjection;
using ElShrine.Modules;
using ElShrine.Modules.Log;
using System.Collections.Concurrent;

namespace ElShrine;

public static class Bootstrapper
{
    private static IContainer? _container;
    public static IContainer Container => _container ?? throw new InvalidOperationException("Bootstrapper not initialized.");

    #region Initialization

    private static bool _initialized;
    private static readonly object _initLock = new();
    private static void InitializeModules(IContainer container, ModuleFlags modules)
    {
        if ((modules & (ModuleFlags.Log | ModuleFlags.VirtualLog)) != 0)
        {
            container.Register<ILogSessionFactory, LogSessionFactory>(ServiceLifetime.Singleton);
            if ((modules & ModuleFlags.Log) != 0)
            {
                container.Register<ILogWriter, LogStore>(ServiceLifetime.Singleton);
                container.Register<ILogReader, LogStore>(ServiceLifetime.Singleton);
            }
            else
            {
                container.Register<ILogWriter, LogVirtualStore>(ServiceLifetime.Singleton);
                container.Register<ILogReader, LogVirtualStore>(ServiceLifetime.Singleton);
            }
        }
        /*if ((modules & ModuleFlags.Plugin) != 0)
            container.TryRegister<IPluginManager, PluginManager>(ServiceLifetime.Singleton);
        if ((modules & ModuleFlags.Option) != 0)
            container.TryRegister<IOptionManager, OptionManager>(ServiceLifetime.Singleton);
        if ((modules & ModuleFlags.Command) != 0)
            container.TryRegister<ICommandManager, CommandsManager>(ServiceLifetime.Singleton);
        if ((modules & ModuleFlags.Localization) != 0)
            container.TryRegister<ILocalizationManager, LocalizationManager>(ServiceLifetime.Singleton);*/
    }
    public static void Initialize(IContainer externalContainer, ModuleFlags modules, Action<IContainer>? configure = null, Action<IServiceProvider>? onReady = null)
    {
        var container = externalContainer;
        lock (_initLock)
        {
            if (_initialized)
                throw new InvalidOperationException("Bootstrapper already initialized.");

            // init container
            configure?.Invoke(container);
            InitializeModules(container, modules);
            _container = container;
            _initialized = true;
        }
        // invoke ready callbacks
        var cbs = _readyCallbacks.OrderByDescending(i => i.Priority);
        foreach (var callback in cbs)
            callback.Action.Invoke(container);
        onReady?.Invoke(container);
        _readyCallbacks.Clear();
    }
    public static void Initialize(ModuleFlags modules, Action<IContainer>? configure = null, Action<IServiceProvider>? onReady = null)
        => Initialize(DependencyInjection.Container.CreateRoot(), modules, configure, onReady);

    #region ReadyCallback
    private sealed record ReadyCallback(int Priority, Action<IServiceProvider> Action);
    private static readonly ConcurrentBag<ReadyCallback> _readyCallbacks = [];
    public static void RegisterReadyCallback(Action<IServiceProvider> callback, int piority = 0)
    {
        lock (_initLock)
        {
            if (_initialized)
                throw new InvalidOperationException("Cannot register callback after initialization.");
        }
        _readyCallbacks.Add(new(piority, callback));
    }
    #endregion

    #endregion

    #region IContainer methods encapsulations
    public static object Resolve(Type serviceType)
    {
        var service = Container.GetService(serviceType) ??
            throw new InvalidOperationException($"Service {serviceType.Name} not resolved.");
        return service;
    }
    public static TIns Resolve<TIns>() where TIns : class
        => (TIns)Resolve(typeof(TIns));
    public static IContainer CreateScope()
        => Container.CreateScope();
    #endregion
}