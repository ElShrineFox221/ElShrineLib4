namespace ElShrine.Modules.Plugin;

public interface IPluginManager : IDisposable
{
    IReadOnlyList<PluginDescriptor> Discovered { get; }

    /// <summary>Host-wide; plugins get a scoped wrapper.</summary>
    IPluginBus Bus { get; }

    /// <summary>Host-wide; plugins get a scoped wrapper.</summary>
    IPluginRegistry Registry { get; }

    bool IsLoaded(string id);

    /// <summary>Loaded plugins are preserved.</summary>
    void Scan();

    bool Load(string id);
    bool Unload(string id);

    void UnloadAll();
}
