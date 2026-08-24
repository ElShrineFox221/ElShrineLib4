namespace ElShrine.Modules.Log;

/// <summary>A logging scope; disposing it writes the scope-end entry.</summary>
public readonly struct LogScope : ILogger, IDisposable
{
    private enum EntryStructuralType
    {
        StartOfScope,
        Normal,
        EndOfScope,
    }

    #region ScopeId
    private static long _nextScopeId;
    private static long AllocateScopeId() => Interlocked.Increment(ref _nextScopeId);
    #endregion

    public string SessionName { get; }
    public long ScopeId { get; }
    public long ParentScopeId { get; }
    public string? FlowKey { get; }
    public int Depth { get; }
    public ILogWriter Writer { get; }

    #region construct
    private LogScope(string sessionName, long parentScopeId, string? flowKey, int depth, ILogWriter writer)
    {
        SessionName = sessionName;
        ScopeId = AllocateScopeId();
        ParentScopeId = parentScopeId;
        FlowKey = flowKey;
        Depth = depth;
        Writer = writer;
    }
    internal static LogScope CreateRootScope(ILogWriter writer, string sessionName)
       => new(sessionName, 0, null, 0, writer);
    public LogScope BeginScope(EntryContent content, string? flowKey = null)
    {
        var child = new LogScope(SessionName, ScopeId, flowKey, Depth + 1, Writer);
        var startEntry = child.CreateEntryData(EntryStructuralType.StartOfScope, flowKey, EntryType.Info, content);
        WriteEntry(startEntry);
        return child;
    }
    #endregion

    #region Log
    private EntryData CreateEntryData(EntryStructuralType structural, string? flowKey, EntryType type, EntryContent content)
    {
        var scopeId = structural is EntryStructuralType.EndOfScope ? -1 : ScopeId;
        var parentScopeId = structural is EntryStructuralType.StartOfScope ? ParentScopeId : ScopeId;

        return new EntryData
        {
            ScopeId = scopeId,
            ParentScopeId = parentScopeId,
            FlowKey = flowKey,
            Type = type,
            Content = content,
            Depth = Depth,
        };
    }
    private void WriteEntry(EntryData entry)
        => Writer.Write(SessionName, entry);
    public void Log(EntryType type, EntryContent content, string? flowKey = null)
    {
        flowKey ??= FlowKey;
        if (flowKey == string.Empty)
            flowKey = null;

        var entry = CreateEntryData(EntryStructuralType.Normal, flowKey, type, content);
        WriteEntry(entry);
    }
    #endregion

    public void Dispose()
    {
        var entry = CreateEntryData(EntryStructuralType.EndOfScope, FlowKey, EntryType.Info, EntryContent.Empty);
        WriteEntry(entry);
    }
}
