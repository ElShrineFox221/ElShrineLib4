using ElShrine.DependencyInjection;
using ElShrine.Modules.Log;

namespace ElShrine.Modules.Plugin;

internal sealed class PluginContext(
    PluginDescriptor descriptor,
    IContainer scope,
    PluginBus bus,
    PluginRegistry registry,
    ILogger? logger) : IPluginContext, IDisposable
{
    private readonly ScopedPluginBus _bus = new(bus, descriptor.Id);
    private readonly ScopedPluginRegistry _registry = new(registry, descriptor.Id);

    public string PluginId { get; } = descriptor.Id;
    public string Folder { get; } = descriptor.Folder;
    public IContainer Services => scope;
    public ILogger? Logger { get; } = logger;
    public IPluginBus Bus => _bus;
    public IPluginRegistry Registry => _registry;

    public void Dispose()
    {
        _bus.Dispose();
        _registry.Dispose();
        scope.Dispose();
    }
}
