namespace ElShrine.Modules.Plugin;

public sealed record PluginOptions
{
    /// <summary>One ALC per direct sub-folder.</summary>
    public string RootFolder { get; init; } = "Plugins";
}
