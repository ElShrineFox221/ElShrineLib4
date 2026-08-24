namespace ElShrine.Modules;

[Flags]
public enum ModuleFlags
{
    None = 0,

    // TickTimer = 1,
    /// <summary>
    /// Structual log service. Logs are stored in a tree structure, and persisted to folders differed by <see cref="Log.LogRunId"/>.
    /// <para>Use <see cref="Log.ILogSessionFactory"/> to get a session. Use <see cref="Log.ILogger"/> to log or open a log scope.</para>
    /// <para>Use <see cref="Log.ILogReader"/> to read logs.</para>
    /// <para>Use <see cref="Log.LogRunRebuild"/> to rebuild logs tree.</para>
    /// </summary>
    Log = 2,
    /// <summary>
    /// Differ from <see cref="Log"/>. Logs will be stored in memory only.
    /// <para><see cref="Log.ILogReader"/> returns only logs of the current run. (<see cref="Log.LogRunId.Current"/>)</para>
    /// Ignored if <see cref="Log"/> is also specified.
    /// </summary>
    VirtualLog = 4,

    // Plugin = 8,

    // Option = 16,
     
    // Command = 32,

    // Localization = 32,

    // All = TickTimer | Log | Plugin | Option | Command | Localization,
}
