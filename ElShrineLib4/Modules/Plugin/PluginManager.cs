using ElShrine.DependencyInjection;
using ElShrine.Modules.Log;
using System.Runtime.Loader;

namespace ElShrine.Modules.Plugin;

public sealed class PluginManager : IPluginManager
{
    private sealed record PluginHost(PluginDescriptor Descriptor, IPlugin Plugin, PluginContext Context);

    private readonly IContainer _root;
    private readonly PluginOptions _options;
    private readonly ILogger? _logger;
    private readonly PluginAlcManager _alc;
    private readonly PluginScanner _scanner;
    private readonly PluginBus _bus;
    private readonly PluginRegistry _registry;
    private readonly Dictionary<string, PluginDescriptor> _descriptors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PluginHost> _hosts = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public PluginManager(IContainer container, ILogSessionFactory? sessionFactory, PluginOptions? options)
    {
        _root = container;
        _options = options ?? new PluginOptions();
        _logger = sessionFactory?.GetOrCreateSession("Plugin");

        var shared = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            typeof(PluginManager).Assembly.GetName().Name!
        };
        _alc = new PluginAlcManager(shared, _logger);
        _scanner = new PluginScanner(_alc, _logger);
        _bus = new PluginBus(_logger);
        _registry = new PluginRegistry();
    }

    public IReadOnlyList<PluginDescriptor> Discovered
    {
        get
        {
            lock (_sync)
                return [.. _descriptors.Values];
        }
    }

    public IPluginBus Bus => _bus;
    public IPluginRegistry Registry => _registry;

    public bool IsLoaded(string id)
    {
        lock (_sync)
            return _hosts.ContainsKey(id);
    }

    public void Scan()
    {
        lock (_sync)
        {
            using var scope = _logger?.BeginScope("Scanning plugins ...");
            var discovered = _scanner.Scan(_options.RootFolder);

            _descriptors.Clear();
            foreach (var descriptor in discovered)
                _descriptors[descriptor.Id] = descriptor;
            // Preserve loaded plugins even if a rescan no longer sees them.
            foreach (var host in _hosts.Values)
                _descriptors[host.Descriptor.Id] = host.Descriptor;

            scope?.Info($"Discovered {discovered.Count} plugin(s); {_hosts.Count} loaded.");
        }
    }

    public bool Load(string id)
    {
        lock (_sync)
        {
            if (_hosts.ContainsKey(id))
                return true;
            if (!_descriptors.TryGetValue(id, out var descriptor))
            {
                _logger?.Warning($"Plugin '{id}' is unknown; scan first.");
                return false;
            }

            using var scope = _logger?.BeginScope("Loading plugin ", id.Parameter(), " ...");

            var alc = _alc.Acquire(descriptor.Folder);
            if (alc is null)
            {
                scope?.Error("Failed to load plugin assemblies.");
                return false;
            }

            IContainer? pluginScope = null;
            try
            {
                var entryType = FindEntryType(alc, descriptor.EntryTypeName)
                    ?? throw new InvalidOperationException($"Entry type '{descriptor.EntryTypeName}' was not found in {descriptor.Folder}.");

                pluginScope = _root.CreateScope(new ScopeOptions { Unregistered = descriptor.Unregistered });
                pluginScope.Register(typeof(IPlugin), entryType, ServiceLifetime.Singleton);

                if (pluginScope.GetService(typeof(IPlugin)) is not IPlugin plugin)
                    throw new InvalidOperationException($"Failed to construct plugin '{descriptor.Id}'.");

                var context = new PluginContext(descriptor, pluginScope, _bus, _registry, _logger);
                plugin.PostLoad(context);

                _hosts[id] = new PluginHost(descriptor, plugin, context);
                scope?.Info("Loaded.");
                return true;
            }
            catch (Exception e)
            {
                scope?.Error(e.ToLogItems());
                try { pluginScope?.Dispose(); } catch { /* ignore cleanup failures */ }
                _alc.Release(descriptor.Folder);
                return false;
            }
        }
    }

    public bool Unload(string id)
    {
        lock (_sync)
        {
            if (!_hosts.TryGetValue(id, out var host))
                return false;

            using var scope = _logger?.BeginScope("Unloading plugin ", id.Parameter(), " ...");

            try
            {
                host.Plugin.PreUnload();
            }
            catch (Exception e)
            {
                _logger?.Error($"Plugin '{id}' {nameof(IPlugin.PreUnload)} failed.");
                _logger?.Error(e.ToLogItems());
            }

            try { host.Context.Dispose(); }
            catch (Exception e) { _logger?.Error(e.ToLogItems()); }

            _hosts.Remove(id);
            _alc.Release(host.Descriptor.Folder);
            scope?.Info("Unloaded.");
            return true;
        }
    }

    public void UnloadAll()
    {
        lock (_sync)
        {
            foreach (var id in _hosts.Keys.ToList())
                Unload(id);
        }
    }

    public void Dispose()
        => UnloadAll();

    private static Type? FindEntryType(AssemblyLoadContext context, string fullName)
    {
        foreach (var assembly in context.Assemblies)
        {
            var type = assembly.GetType(fullName, throwOnError: false);
            if (type is not null)
                return type;
        }
        return null;
    }
}
