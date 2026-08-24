using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace ElShrine.Modules.Log;

/// <summary>
/// Default implementation of <see cref="ILogWriter"/> and <see cref="ILogReader"/>.
/// Persists entries as newline-delimited JSON (one file per session) under
/// <see cref="LogRunId.Current"/>'s folder. Writes are non-blocking: a single background drain
/// worker performs all disk IO; current-run reads are served from a write-through in-memory cache.
/// </summary>
public class LogStore : ILogWriter, ILogReader, IAsyncDisposable, IDisposable
{
    #region Nested

    /// <summary>
    /// Serializes <see cref="EntryContent"/> as a plain JSON array of <see cref="LogItem"/>s.
    /// Relies on JSON string escaping for embedded newlines inside item text.
    /// </summary>
    private sealed class EntryContentConverter : JsonConverter<EntryContent>
    {
        public override EntryContent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var items = JsonSerializer.Deserialize<LogItem[]>(ref reader, options);
            return items != null && items.Length > 0
                ? new EntryContent(items)
                : EntryContent.Empty;
        }
        public override void Write(Utf8JsonWriter writer, EntryContent value, JsonSerializerOptions options)
        {
            if (value.Items.Length == 0)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }
            JsonSerializer.Serialize(writer, value.Items, options);
        }
    }
    #endregion
    private const string RAWLOG_EXTENSION = "rawlog";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IgnoreReadOnlyProperties = true,
        Converters = { new EntryContentConverter() }
    };
    private static readonly TimeSpan DisposeShortTimeout = TimeSpan.FromSeconds(1);

    private readonly object _disposeLock = new();
    private volatile bool _disposed;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<EntryData>> _currentCache = new();

    public string BaseDir { get; }
    private string CDir => (this as ILogIO).CurrentDir;

    public LogStore(string? baseDir = null)
    {
        BaseDir = baseDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(CDir);

        _channel = Channel.CreateUnbounded<ChannelItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _drainCts = new CancellationTokenSource();
        _drainTask = RunDrainAsync(_drainCts.Token);
    }

    #region ILogWriter
    public void Write(string sessionName, EntryData entry)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _currentCache.GetOrAdd(sessionName, static _ => new ConcurrentQueue<EntryData>()).Enqueue(entry);
        ObjectDisposedException.ThrowIf(!_channel.Writer.TryWrite(new EntryWrite(sessionName, entry)), this);
    }
    public ValueTask FlushAsync(CancellationToken ct = default)
    {
        if (_disposed)
            return ValueTask.FromException(new ObjectDisposedException(nameof(LogStore)));

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_channel.Writer.TryWrite(new FlushMarker(tcs)))
            return ValueTask.FromException(new ObjectDisposedException(nameof(LogStore)));

        return ct.CanBeCanceled
            ? new ValueTask(tcs.Task.WaitAsync(ct))
            : new ValueTask(tcs.Task);
    }
    #endregion

    #region Drain worker
    private abstract record ChannelItem;
    private sealed record EntryWrite(string SessionName, EntryData Entry) : ChannelItem;
    private sealed record FlushMarker(TaskCompletionSource Tcs) : ChannelItem;

    private readonly Channel<ChannelItem> _channel;
    private readonly Task _drainTask;
    private readonly CancellationTokenSource _drainCts;
    // Accessed only by the drain worker and (after the drain completes) the disposer.
    private readonly Queue<(Exception Ex, EntryData Entry, string Session)> _pendingErrors = new();
    private readonly Dictionary<string, StreamWriter> _writers = [];
    private StreamWriter GetOrCreateWriter(string sessionName)
    {
        if (_writers.TryGetValue(sessionName, out var existing))
            return existing;

        var path = Path.ChangeExtension(Path.Combine(CDir, sessionName), RAWLOG_EXTENSION);
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = false };
        _writers.Add(sessionName, writer);
        return writer;
    }
    private LogWriteException DrainPendingErrors()
    {
        var errors = new List<Exception>();
        var lost = new List<EntryData>();
        var affected = new HashSet<string>(StringComparer.Ordinal);
        while (_pendingErrors.TryDequeue(out var triple))
        {
            errors.Add(triple.Ex);
            lost.Add(triple.Entry);
            affected.Add(triple.Session);
        }

        return new LogWriteException(
            $"{errors.Count} error(s) occurred while persisting {lost.Count} log entr{(lost.Count == 1 ? "y" : "ies")} across {affected.Count} session(s).",
            errors, lost, [.. affected]);
    }
    private async Task RunDrainAsync(CancellationToken ct)
    {
        var reader = _channel.Reader;
        var batch = new List<ChannelItem>(32);
        try
        {
            while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                batch.Clear();
                while (reader.TryRead(out var item))
                    batch.Add(item);

                var flushMarkers = new List<FlushMarker>(batch.Count);
                var entriesBySession = new Dictionary<string, List<EntryData>>(StringComparer.Ordinal);
                for (var i = 0; i < batch.Count; i++)
                {
                    switch (batch[i])
                    {
                        case EntryWrite ew:
                            if (!entriesBySession.TryGetValue(ew.SessionName, out var list))
                                entriesBySession[ew.SessionName] = list = [];
                            list.Add(ew.Entry);
                            break;
                        case FlushMarker fm:
                            flushMarkers.Add(fm);
                            break;
                    }
                }

                foreach (var (sessionName, entries) in entriesBySession)
                {
                    StreamWriter writer;
                    // get writer
                    try { writer = GetOrCreateWriter(sessionName); }
                    catch (Exception ex)
                    {
                        foreach (var entry in entries)
                            _pendingErrors.Enqueue((ex, entry, sessionName));
                        continue;
                    }
                    // handle write
                    foreach (var entry in entries)
                    {
                        try
                        {
                            var json = JsonSerializer.Serialize(entry, _jsonOptions);
                            writer.WriteLine(json);
                        }
                        catch (Exception ex)
                        {
                            _pendingErrors.Enqueue((ex, entry, sessionName));
                        }
                    }
                    // flush
                    try { writer.Flush(); }
                    catch (Exception ex)
                    {
                        foreach (var entry in entries)
                            _pendingErrors.Enqueue((ex, entry, sessionName));
                    }
                }

                foreach (var fm in flushMarkers)
                {
                    if (_pendingErrors.Count == 0)
                        fm.Tcs.TrySetResult();
                    else
                        fm.Tcs.TrySetException(DrainPendingErrors());
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    #endregion

    #region ILogReader
    private string BuildSessionPath(LogRunId runId, string sessionName)
        => Path.ChangeExtension(Path.Combine(BaseDir, runId.ToString(), sessionName), RAWLOG_EXTENSION);
    private static List<EntryData> ReadSessionFile(string path)
    {
        if (!File.Exists(path))
            return [];

        var list = new List<EntryData>();
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var entry = JsonSerializer.Deserialize<EntryData>(line, _jsonOptions);
            if (entry != null)
                list.Add(entry);
        }
        return list;
    }

    public IEnumerable<LogRunId> EnumerateRuns()
    {
        if (!Directory.Exists(BaseDir))
            yield break;

        var runs = new HashSet<LogRunId>();
        foreach (var dir in Directory.EnumerateDirectories(BaseDir))
        {
            var folderName = Path.GetFileName(dir);
            if (LogRunId.TryParse(folderName, out var id))
                runs.Add(id);
        }
        foreach(var run in runs.OrderDescending())
            yield return run;
    }
    public IEnumerable<string> EnumerateSessions(LogRunId runId)
    {
        var dir = Path.Combine(BaseDir, runId.ToString());
        if (!Directory.Exists(dir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(dir, $"*.{RAWLOG_EXTENSION}"))
            yield return Path.GetFileNameWithoutExtension(file);
    }
    public IReadOnlyList<EntryData> ReadSession(LogRunId runId, string sessionName)
    {
        if (runId == LogRunId.Current)
        {
            if (_currentCache.TryGetValue(sessionName, out var queue))
                return [.. queue];
            return [];
        }
        return ReadSessionFile(BuildSessionPath(runId, sessionName));
    }
    #endregion

    #region IDisposable / IAsyncDisposable
    private void DoSyncDispose()
    {
        foreach (var (_, writer) in _writers)
            writer.Dispose();
        _writers.Clear();
        _currentCache.Clear();
        _drainCts.Dispose();
    }
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _channel.Writer.TryComplete();
        try
        {
            if (!_drainTask.Wait(DisposeShortTimeout))
                _drainCts.Cancel();
        }
        catch (AggregateException) { }
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

        _channel.Writer.TryComplete();
        try { await _drainTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
        DoSyncDispose();

        GC.SuppressFinalize(this);
        if (_pendingErrors.Count > 0)
            throw DrainPendingErrors();
    }
    #endregion
}
