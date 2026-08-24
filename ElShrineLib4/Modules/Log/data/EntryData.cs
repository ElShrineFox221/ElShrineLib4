namespace ElShrine.Modules.Log;

/// <summary>
/// Scope boundary encoding (ScopeId vs ParentScopeId):
/// <list type="bullet">
/// <item>Scope start: ScopeId != ParentScopeId</item>
/// <item>Normal line: ScopeId == ParentScopeId</item>
/// <item>Scope end: ScopeId == -1</item>
/// </list>
/// </summary>
public sealed record EntryData
{
    #region Data

    #region Core
    private static long _nextId;
    public long Id { get; init; } = Interlocked.Increment(ref _nextId);
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public int ThreadId { get; init; } = Environment.CurrentManagedThreadId;
    public string? FlowKey { get; init; }
    public long ScopeId { get; init; }
    public long ParentScopeId { get; init; }
    public EntryContent Content { get; init; } = EntryContent.Empty;
    #endregion

    #region Extra
    public int Depth { get; init; } = 0;
    public EntryType Type { get; init; }
    #endregion

    #endregion
}
