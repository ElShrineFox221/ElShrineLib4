using System.Text.Json;

namespace ElShrine.Modules.Plugin;

/// <summary>
/// A cross-ALC message. The payload is a neutral <see cref="JsonElement"/> so that
/// plugins never exchange ALC-bound type instances.
/// </summary>
public readonly record struct PluginMessage(JsonElement Payload, string? SenderId);

public interface IPluginBus
{
    /// <summary>Dispose the token to unsubscribe.</summary>
    IDisposable Subscribe(string topic, Action<PluginMessage> handler);

    void Publish(string topic, PluginMessage message);
}
