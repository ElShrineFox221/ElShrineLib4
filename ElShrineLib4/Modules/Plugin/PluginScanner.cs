using ElShrine.DependencyInjection;
using ElShrine.Modules.Log;
using ElShrine.Reflection;

namespace ElShrine.Modules.Plugin;

internal sealed class PluginScanner(PluginAlcManager alcManager, ILogger? logger)
{
    private readonly PluginAlcManager _alcManager = alcManager;
    private readonly ILogger? _logger = logger;

    public IReadOnlyList<PluginDescriptor> Scan(string rootFolder)
    {
        var root = Path.GetFullPath(rootFolder);
        Directory.CreateDirectory(root);
        var folders = Directory.GetDirectories(root);
        _logger?.Info($"Found {folders.Length} plugin folder(s).");

        var candidates = new List<ScannedPlugin>();
        foreach (var folder in folders)
            candidates.AddRange(ScanFolder(folder));

        return ValidatePackages(candidates);
    }

    private List<ScannedPlugin> ScanFolder(string folder)
    {
        var context = _alcManager.Acquire(folder);
        if (context is null)
            return [];

        try
        {
            var folderName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var results = new List<ScannedPlugin>();
            foreach (var pair in context.GetTypesByAttribute<PluginAttribute>(inherit: false))
            {
                var type = pair.Key;
                var attribute = pair.Value[0];
                if (type.IsAbstract || string.IsNullOrWhiteSpace(type.FullName))
                    continue;

                results.Add(new ScannedPlugin(
                    PackageId: string.IsNullOrWhiteSpace(attribute.PackageId) ? folderName : attribute.PackageId,
                    Name: string.IsNullOrWhiteSpace(attribute.Name) ? type.Name : attribute.Name,
                    Author: attribute.Author,
                    Version: attribute.Version,
                    Description: attribute.Description,
                    Folder: folder,
                    EntryTypeName: type.FullName,
                    Unregistered: attribute.Unregistered));
            }
            return results;
        }
        catch (Exception e)
        {
            _logger?.Error(e.ToLogItems());
            return [];
        }
        finally
        {
            _alcManager.Release(folder);
        }
    }

    /// <summary>
    /// Global validation: one package per folder with a uniform PackageId; a PackageId may
    /// only be claimed by one folder (first wins); names must be unique within a package.
    /// </summary>
    private List<PluginDescriptor> ValidatePackages(List<ScannedPlugin> candidates)
    {
        var packages = candidates
            .GroupBy(p => p.Folder, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Package(g.Key, [.. g]))
            .OrderBy(p => p.Folder, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var byPackageId = new Dictionary<string, Package>(StringComparer.Ordinal);
        foreach (var package in packages)
        {
            var ids = package.Plugins.Select(p => p.PackageId).Distinct(StringComparer.Ordinal).ToList();
            if (ids.Count != 1)
            {
                _logger?.Error($"Folder {package.Folder} declares inconsistent PackageIds; package invalid.");
                continue;
            }
            package.PackageId = ids[0];

            if (byPackageId.TryGetValue(package.PackageId, out var kept))
            {
                _logger?.Warning($"PackageId '{package.PackageId}' conflict: {package.Folder} ignored, keeping {kept.Folder}.");
                continue;
            }
            byPackageId[package.PackageId] = package;
        }

        var descriptors = new List<PluginDescriptor>();
        foreach (var package in byPackageId.Values)
        {
            var names = package.Plugins.Select(p => p.Name).ToList();
            if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.Ordinal).Count() != names.Count)
            {
                _logger?.Error($"Package '{package.PackageId}' has empty or duplicate plugin names; package invalid.");
                continue;
            }

            foreach (var plugin in package.Plugins)
            {
                descriptors.Add(new PluginDescriptor(
                    Folder: plugin.Folder,
                    EntryTypeName: plugin.EntryTypeName,
                    Unregistered: plugin.Unregistered,
                    Manifest: new PluginManifest(plugin.PackageId, plugin.Name, plugin.Author, plugin.Version, plugin.Description)));
            }
        }
        return descriptors;
    }

    private sealed record ScannedPlugin(
        string PackageId,
        string Name,
        string Author,
        string Version,
        string Description,
        string Folder,
        string EntryTypeName,
        UnregisteredPolicy Unregistered);

    private sealed class Package(string folder, List<ScannedPlugin> plugins)
    {
        public string Folder { get; } = folder;
        public List<ScannedPlugin> Plugins { get; } = plugins;
        public string PackageId { get; set; } = string.Empty;
    }
}
