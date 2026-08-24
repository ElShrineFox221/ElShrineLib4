namespace ElShrine.Modules.Log;

/// <summary>
/// Aggregated write/flush error produced by the drain worker and surfaced via
/// <see cref="ILogWriter.FlushAsync"/> or <see cref="LogStore.DisposeAsync"/>.
/// </summary>
public sealed class LogWriteException(string message,
    IReadOnlyList<Exception> innerErrors,
    IReadOnlyList<EntryData> lostEntries,
    IReadOnlyList<string> affectedSessions) : Exception(message, innerErrors is { Count: > 0 } ? innerErrors[0] : null)
{
    public IReadOnlyList<Exception> InnerErrors { get; } = innerErrors ?? [];
    public IReadOnlyList<EntryData> LostEntries { get; } = lostEntries ?? [];
    public IReadOnlyList<string> AffectedSessions { get; } = affectedSessions ?? [];
}
