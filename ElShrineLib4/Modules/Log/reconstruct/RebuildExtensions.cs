namespace ElShrine.Modules.Log;

public static class RebuildExtensions
{
    /// <summary>Opens a run for rebuild, caching its session list.</summary>
    public static LogRunRebuild OpenRun(this ILogReader reader, LogRunId runId)
        => new(reader, runId);
}
