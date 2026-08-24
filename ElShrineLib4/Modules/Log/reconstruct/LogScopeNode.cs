namespace ElShrine.Modules.Log;

/// <summary>
/// One reconstructed scope in the timeline tree.
/// <see cref="Children"/> is the interleaved sequence of direct <see cref="LogScopeChild"/> items
/// (Lines and child Scopes).
/// </summary>
public sealed class LogScopeNode
{
    public long ScopeId { get; }
    public int Depth { get; }
    public EntryData StartEntry { get; }
    /// <summary>null when the scope was never closed (crash or still running).</summary>
    public EntryData? EndEntry { get; internal set; }
    public TimeSpan? Duration => EndEntry is null ? null : TimeSpan.FromMilliseconds(EndEntry.Timestamp - StartEntry.Timestamp);
    internal List<LogScopeChild> ChildrenInternal { get; } = [];
    public IReadOnlyList<LogScopeChild> Children => ChildrenInternal;
    /// <summary>Direct Line children only; structural start/end entries excluded.</summary>
    public EntryStats LocalStats { get; internal set; }
    /// <summary>Subtree total: <see cref="LocalStats"/> plus all descendants' <see cref="TotalStats"/>.</summary>
    public EntryStats TotalStats { get; internal set; }

    internal LogScopeNode(EntryData startEntry)
    {
        StartEntry = startEntry;
        ScopeId = startEntry.ScopeId;
        Depth = startEntry.Depth;
    }
}
