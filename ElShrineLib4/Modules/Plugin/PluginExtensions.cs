using System.Text.Json;

namespace ElShrine.Modules.Plugin;

public static class PluginExtensions
{
    #region Bus
    /// <summary>Serializes the payload before publishing.</summary>
    public static void Publish<T>(this IPluginBus bus, string topic, T payload)
        => bus.Publish(topic, new PluginMessage(JsonSerializer.SerializeToElement(payload), null));

    public static T? Deserialize<T>(this in PluginMessage message)
        => message.Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? default
            : message.Payload.Deserialize<T>();
    #endregion

    #region Registry
    public static IEnumerable<T> GetProviders<T>(this IPluginRegistry registry) where T : class
        => registry.GetProviders(typeof(T)).Cast<T>();

    public static T? GetProvider<T>(this IPluginRegistry registry) where T : class
        => registry.GetProviders<T>().FirstOrDefault();
    #endregion
}
