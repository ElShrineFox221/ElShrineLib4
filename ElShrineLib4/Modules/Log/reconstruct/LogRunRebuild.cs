namespace ElShrine.Modules.Log;

/// <summary>
/// Browse-level view over a single run. Caches the session list and rebuilds
/// individual sessions on demand via <see cref="RebuildSession"/>.
/// </summary>
public sealed class LogRunRebuild(ILogReader reader, LogRunId runId)
{
    public LogRunId RunId { get; } = runId;
    private ILogReader Reader { get; } = reader;

    public IReadOnlyList<string> SessionNames { get; } = [.. reader.EnumerateSessions(runId)];

    public LogSessionRebuild RebuildSession(string sessionName)
    {
        var entries = Reader.ReadSession(RunId, sessionName);
        return new LogSessionRebuild(RunId, sessionName, entries);
    }
    public IEnumerable<LogSessionRebuild> RebuildAll()
    {
        foreach (var name in SessionNames)
            yield return RebuildSession(name);
    }
}
