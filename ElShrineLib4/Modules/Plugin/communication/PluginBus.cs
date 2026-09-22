using ElShrine.Modules.Log;

namespace ElShrine.Modules.Plugin;

/// <summary>Per-handler failures are isolated.</summary>
internal sealed class PluginBus(ILogger? logger) : IPluginBus
{
    private sealed class Subscription(PluginBus bus, string topic, Action<PluginMessage> handler) : IDisposable
    {
        public readonly Action<PluginMessage> Handler = handler;
        private readonly PluginBus _bus = bus;
        private readonly string _topic = topic;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _bus.Remove(_topic, this);
        }
    }

    private readonly ILogger? _logger = logger;
    private readonly Dictionary<string, List<Subscription>> _topics = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public IDisposable Subscribe(string topic, Action<PluginMessage> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new Subscription(this, topic, handler);
        lock (_lock)
        {
            if (!_topics.TryGetValue(topic, out var list))
                _topics[topic] = list = [];
            list.Add(subscription);
        }
        return subscription;
    }

    private void Remove(string topic, Subscription subscription)
    {
        lock (_lock)
        {
            if (!_topics.TryGetValue(topic, out var list))
                return;
            list.Remove(subscription);
            if (list.Count == 0)
                _topics.Remove(topic);
        }
    }

    public void Publish(string topic, PluginMessage message)
    {
        Subscription[] subscribers;
        lock (_lock)
        {
            if (!_topics.TryGetValue(topic, out var list) || list.Count == 0)
                return;
            subscribers = [.. list];
        }

        foreach (var subscriber in subscribers)
        {
            try
            {
                subscriber.Handler(message);
            }
            catch (Exception e)
            {
                _logger?.Error(e.ToLogItems());
            }
        }
    }
}

/// <summary>Every subscription is revoked when disposed.</summary>
internal sealed class ScopedPluginBus(PluginBus inner, string ownerId) : IPluginBus, IDisposable
{
    private readonly PluginBus _inner = inner;
    private readonly string _ownerId = ownerId;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly object _lock = new();

    public IDisposable Subscribe(string topic, Action<PluginMessage> handler)
    {
        var token = _inner.Subscribe(topic, handler);
        lock (_lock)
            _subscriptions.Add(token);
        return token;
    }

    public void Publish(string topic, PluginMessage message)
        => _inner.Publish(topic, message with { SenderId = _ownerId });

    public void Dispose()
    {
        List<IDisposable> tokens;
        lock (_lock)
        {
            tokens = [.. _subscriptions];
            _subscriptions.Clear();
        }
        foreach (var token in tokens)
            token.Dispose();
    }
}
