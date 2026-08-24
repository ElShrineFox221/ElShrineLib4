namespace ElShrine.Modules.Log;

/// <summary>
/// Write side of the log store. Bound to <see cref="LogRunId.Current"/> (current process run).
/// <see cref="Write"/> returns immediately; durable persistence is awaited explicitly via
/// <see cref="FlushAsync"/>.
/// </summary>
public interface ILogWriter : ILogIO, IAsyncDisposable, IDisposable
{
    /// <summary>Enqueues an entry and returns immediately.</summary>
    /// <exception cref="ObjectDisposedException"/>
    void Write(string sessionName, EntryData entry);

    /// <summary>
    /// Waits for every entry enqueued before this call to be persisted. Faults with
    /// <see cref="LogWriteException"/> if any error accumulated since the last successful flush.
    /// </summary>
    /// <exception cref="LogWriteException"/>
    /// <exception cref="OperationCanceledException"/>
    ValueTask FlushAsync(CancellationToken ct = default);
}
