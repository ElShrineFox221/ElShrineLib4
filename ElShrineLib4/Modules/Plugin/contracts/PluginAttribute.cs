using ElShrine.DependencyInjection;
using ElShrine.Reflection;

namespace ElShrine.Modules.Plugin;

/// <summary>Also validates that the decorated type is assignable to <see cref="IPlugin"/>.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PluginAttribute() : AssignableValidation<IPlugin>(AssignmentKind.AssignableTo)
{
    /// <summary>Empty falls back to the containing folder name.</summary>
    public string PackageId = string.Empty;
    /// <summary>Unique within its package. Empty falls back to the type name.</summary>
    public string Name = string.Empty;
    public string Author = string.Empty;
    public string Version = string.Empty;
    public string Description = string.Empty;
    public UnregisteredPolicy Unregistered = UnregisteredPolicy.Reject;
}
