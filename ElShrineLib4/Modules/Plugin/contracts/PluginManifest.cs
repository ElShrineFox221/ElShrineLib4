namespace ElShrine.Modules.Plugin;

public sealed record PluginManifest(
    string PackageId,
    string Name,
    string Author,
    string Version,
    string Description);
