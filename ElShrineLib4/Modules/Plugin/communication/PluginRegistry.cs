namespace ElShrine.Modules.Plugin;

internal sealed class PluginRegistry : IPluginRegistry
{
    private sealed record Provider(object Instance, string? OwnerId);

    private sealed class Handle(PluginRegistry registry, Type contract, Provider provider) : IDisposable
    {
        private readonly PluginRegistry _registry = registry;
        private readonly Type _contract = contract;
        private readonly Provider _provider = provider;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _registry.Remove(_contract, _provider);
        }
    }

    private readonly Dictionary<Type, List<Provider>> _providers = [];
    private readonly object _lock = new();

    public IDisposable Provide(Type contract, object implementation)
        => Provide(contract, implementation, null);

    public IDisposable Provide(Type contract, object implementation, string? ownerId)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(implementation);
        if (!contract.IsInstanceOfType(implementation))
            throw new ArgumentException(
                $"Implementation {implementation.GetType().FullName} is not assignable to contract {contract.FullName}.",
                nameof(implementation));

        var provider = new Provider(implementation, ownerId);
        lock (_lock)
        {
            if (!_providers.TryGetValue(contract, out var list))
                _providers[contract] = list = [];
            list.Add(provider);
        }
        return new Handle(this, contract, provider);
    }

    private void Remove(Type contract, Provider provider)
    {
        lock (_lock)
        {
            if (!_providers.TryGetValue(contract, out var list))
                return;
            list.Remove(provider);
            if (list.Count == 0)
                _providers.Remove(contract);
        }
    }

    public IEnumerable<object> GetProviders(Type contract)
    {
        Provider[] snapshot;
        lock (_lock)
        {
            if (!_providers.TryGetValue(contract, out var list) || list.Count == 0)
                return [];
            snapshot = [.. list];
        }
        return snapshot.Select(p => p.Instance);
    }
}

/// <summary>Every provided capability is revoked when disposed.</summary>
internal sealed class ScopedPluginRegistry(PluginRegistry inner, string ownerId) : IPluginRegistry, IDisposable
{
    private readonly PluginRegistry _inner = inner;
    private readonly string _ownerId = ownerId;
    private readonly List<IDisposable> _handles = [];
    private readonly object _lock = new();

    public IDisposable Provide(Type contract, object implementation)
    {
        var handle = _inner.Provide(contract, implementation, _ownerId);
        lock (_lock)
            _handles.Add(handle);
        return handle;
    }

    public IEnumerable<object> GetProviders(Type contract) => _inner.GetProviders(contract);

    public void Dispose()
    {
        List<IDisposable> handles;
        lock (_lock)
        {
            handles = [.. _handles];
            _handles.Clear();
        }
        foreach (var handle in handles)
            handle.Dispose();
    }
}
