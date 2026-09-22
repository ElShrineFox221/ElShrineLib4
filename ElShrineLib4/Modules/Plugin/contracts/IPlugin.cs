namespace ElShrine.Modules.Plugin;

/// <summary>
/// Implementations must be non-abstract classes decorated with <see cref="PluginAttribute"/>.
/// <para>Callbacks are individually guarded: an exception is logged and the plugin is not
/// registered, without affecting other plugins.</para>
/// </summary>
public interface IPlugin
{
    void PostLoad(IPluginContext context);
    void PreUnload();
}
