using ElShrine.DependencyInjection;
using ElShrine.Modules.Log;

namespace ElShrine.Modules.Plugin;

public interface IPluginContext
{
    string PluginId { get; }
    string Folder { get; }

    /// <summary>Isolated scope for this plugin.</summary>
    IContainer Services { get; }

    ILogger? Logger { get; }

    /// <summary>Scoped to this plugin; subscriptions are removed on unload.</summary>
    IPluginBus Bus { get; }

    /// <summary>Scoped to this plugin; providers are removed on unload.</summary>
    IPluginRegistry Registry { get; }
}
