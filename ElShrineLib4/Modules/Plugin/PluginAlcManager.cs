using ElShrine.Modules.Log;
using ElShrine.Reflection;
using System.Reflection;
using System.Runtime.Loader;

namespace ElShrine.Modules.Plugin;

/// <summary>
/// Owns the collectible contexts, keyed by plugin folder, with reference counting so
/// several plugins sharing a folder keep the context alive until the last is released.
/// </summary>
internal sealed class PluginAlcManager(IReadOnlySet<string> sharedNames, ILogger? logger)
{
    /// <summary>
    /// Collectible load context for one plugin folder. Assemblies on the shared list, and any
    /// assembly already present in the default context, are resolved from the default context so
    /// contract types keep a single identity; everything else is resolved from the plugin folder.
    /// </summary>
    private sealed class PluginLoadContext(string folder, IReadOnlySet<string> sharedNames)
        : AssemblyLoadContext($"Plugin:{Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}:{Guid.NewGuid():N}", isCollectible: true)
    {
        private readonly AssemblyDependencyResolver _resolver = new(folder);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            if (name is not null && sharedNames.Contains(name))
                return null;

            // Unify with the default context to keep type identity for shared dependencies.
            if (Default.Assemblies.Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName)))
                return null;

            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            if (path is not null)
            {
                try { return LoadFromAssemblyPath(path); }
                catch { }
            }

            // deps.json may be missing; fall back to a flat probe in the folder.
            if (name is not null)
            {
                try
                {
                    var flat = Path.Combine(folder, name + ".dll");
                    if (File.Exists(flat))
                        return LoadFromAssemblyPath(Path.GetFullPath(flat));
                }
                catch { }
            }

            // Let the runtime resolve from the default context (framework, app deps).
            return null;
        }
    }

    private sealed class Entry
    {
        public required PluginLoadContext Context;
        public int RefCount;
    }

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public AssemblyLoadContext? Acquire(string folder)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(folder, out var existing))
            {
                existing.RefCount++;
                return existing.Context;
            }

            var context = new PluginLoadContext(folder, sharedNames);
            if (!TryInitialize(context, folder, logger))
            {
                context.Unload();
                return null;
            }

            _entries[folder] = new Entry { Context = context, RefCount = 1 };
            return context;
        }
    }

    /// <summary>Unloads the context once the last reference is gone.</summary>
    public void Release(string folder)
    {
        AssemblyLoadContext? context = null;
        lock (_lock)
        {
            if (!_entries.TryGetValue(folder, out var entry))
                return;
            if (--entry.RefCount > 0)
                return;
            _entries.Remove(folder);
            context = entry.Context;
            entry = null!;
        }

        context?.Unload();
    }

    private static bool TryInitialize(AssemblyLoadContext context, string folder, ILogger? logger)
    {
        var dlls = Directory.GetFiles(folder, "*.dll", SearchOption.TopDirectoryOnly);
        if (dlls.Length == 0)
        {
            logger?.Warning($"Folder {folder} has no assemblies.");
            return false;
        }

        var roots = new List<AssemblyName>(dlls.Length);
        foreach (var dll in dlls)
        {
            try { roots.Add(AssemblyName.GetAssemblyName(dll)); }
            catch { logger?.Warning($"Skipped non-managed file {Path.GetFileName(dll)}."); }
        }
        if (roots.Count == 0)
        {
            logger?.Warning($"Folder {folder} has no loadable assemblies.");
            return false;
        }

        if (!context.LoadAllReferencesAssemblies(roots, logger))
        {
            logger?.Warning($"Folder {folder} has invalid assemblies.");
            return false;
        }

        foreach (var assembly in context.Assemblies.ToArray())
        {
            if (!assembly.Validate())
            {
                logger?.Warning($"Assembly {assembly.GetName().Name} failed validation.");
                return false;
            }
        }
        return true;
    }
}
