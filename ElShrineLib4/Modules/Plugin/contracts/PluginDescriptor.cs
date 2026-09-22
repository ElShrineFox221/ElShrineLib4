using ElShrine.DependencyInjection;

namespace ElShrine.Modules.Plugin;

/// <summary>Holds no ALC-bound references, so it stays valid across unload.</summary>
public sealed record PluginDescriptor(
    string Folder,          // also the ALC key
    string EntryTypeName,
    UnregisteredPolicy Unregistered,
    PluginManifest Manifest)
{
    public string Id => $"{Manifest.PackageId}::{Manifest.Name}";
}
