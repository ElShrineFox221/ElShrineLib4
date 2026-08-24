using System.Collections.Concurrent;

namespace ElShrine.Modules.Log;

/// <summary>
/// In-memory <see cref="ILogReader"/>/<see cref="ILogWriter"/>; persists nothing to disk.
/// </summary>
public class LogVirtualStore(string? baseDir = null) : ILogReader, ILogWriter
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<EntryData>> _currentCache = new();
    private readonly object _disposeLock = new();
    private volatile bool _disposed;
    public string BaseDir { get; } = baseDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    #region ILogReader
    public IEnumerable<LogRunId> EnumerateRuns()
    {
        yield return LogRunId.Current;
    }
    public IEnumerable<string> EnumerateSessions(LogRunId runId)
    {
        if (runId != LogRunId.Current) 
            yield break;
        foreach(var sName in _currentCache.Keys)
            yield return sName;
    }
    public IReadOnlyList<EntryData> ReadSession(LogRunId runId, string sessionName)
    {
        if (runId == LogRunId.Current && _currentCache.TryGetValue(sessionName, out var values))
            return [.. values];
        return [];
    }
    #endregion

    #region ILogWriter
    public void Write(string sessionName, EntryData entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _currentCache.GetOrAdd(sessionName, static _ => new ConcurrentQueue<EntryData>()).Enqueue(entry);
    }
    public async ValueTask FlushAsync(CancellationToken ct = default)
        => await ValueTask.CompletedTask;
    #endregion

    #region IDisposable / IAsyncDisposable
    private void DoSyncDispose()
        => _currentCache.Clear();
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        DoSyncDispose();

        GC.SuppressFinalize(this);
    }
    public async ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        await ValueTask.CompletedTask;
        DoSyncDispose();

        GC.SuppressFinalize(this);
    }
    #endregion
}
