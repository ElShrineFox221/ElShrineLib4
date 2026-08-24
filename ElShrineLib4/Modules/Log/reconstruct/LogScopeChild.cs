namespace ElShrine.Modules.Log;

/// <summary>
/// Discriminated union of a scope's direct children: a content <see cref="Line"/> or a nested <see cref="Scope"/>.
/// </summary>
public abstract record LogScopeChild
{
    internal abstract EntryData GetEntryData();
    private LogScopeChild() { }

    public sealed record Line(EntryData Entry) : LogScopeChild
    {
        internal override EntryData GetEntryData()
            => Entry;
    }
    public sealed record Scope(LogScopeNode Node) : LogScopeChild
    {
        internal override EntryData GetEntryData()
            => Node.StartEntry;
    }
}
