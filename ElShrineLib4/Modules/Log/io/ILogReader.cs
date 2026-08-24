namespace ElShrine.Modules.Log;

/// <summary>
/// Read side of the log store. Current-run reads reflect every <see cref="ILogWriter.Write"/>
/// immediately.
/// </summary>
public interface ILogReader : ILogIO
{
    /// <summary>Enumerates runs, newest first.</summary>
    IEnumerable<LogRunId> EnumerateRuns();

    /// <summary>Lists session names in a run. Empty if the run has no sessions.</summary>
    IEnumerable<string> EnumerateSessions(LogRunId runId);

    /// <summary>Reads a single session in write order. Empty when the session does not exist.</summary>
    IReadOnlyList<EntryData> ReadSession(LogRunId runId, string sessionName);

    /// <summary>Reads every session of a run, keyed by session name.</summary>
    IReadOnlyDictionary<string, IReadOnlyList<EntryData>> ReadAll(LogRunId runId)
    {
        var dict = new Dictionary<string, IReadOnlyList<EntryData>>(StringComparer.Ordinal);
        foreach (var name in EnumerateSessions(runId))
            dict[name] = ReadSession(runId, name);
        return dict;
    }
}
