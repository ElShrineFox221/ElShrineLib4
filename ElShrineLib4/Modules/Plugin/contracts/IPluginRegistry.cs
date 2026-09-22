namespace ElShrine.Modules.Plugin;

/// <summary>
/// Cross-plugin capability registry. Contracts must be types from the default
/// context (i.e. the host assembly or assemblies on the shared list) so that
/// provider and consumer agree on type identity.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>Dispose the handle to revoke.</summary>
    IDisposable Provide(Type contract, object implementation);

    IEnumerable<object> GetProviders(Type contract);
}
