namespace ElShrine.Modules.Log;

/// <summary>
/// Rebuilds a single session's flat <see cref="EntryData"/> timeline into a scope tree for rendering.
/// The session acts as the implicit root scope: <see cref="Children"/> is the top-level interleaved
/// timeline (Lines + Scopes). All stats exclude structural scope-start/end entries.
/// </summary>
public sealed class LogSessionRebuild
{
    public LogRunId RunId { get; }
    public string SessionName { get; }
    public IReadOnlyList<LogScopeChild> Children { get; }
    public EntryStats TotalStats { get; }
    /// <summary>End entries with no matching open scope (always anomalous).</summary>
    public IReadOnlyList<EntryData> OrphanEnds { get; }
    /// <summary>Scope-start entries whose ScopeId already existed (always anomalous).</summary>
    public IReadOnlyList<EntryData> DuplicatedStarts { get; }
    public LogSessionRebuild(LogRunId runId, string sessionName, IReadOnlyList<EntryData> entries)
    {
        RunId = runId;
        SessionName = sessionName;
        
        // collect all scope nodes
        var rootChildren = new List<LogScopeChild>();
        var normalEntries = new List<EntryData>();
        var endEntries = new List<EntryData>();
        var scopeNodesDict = new Dictionary<long, LogScopeNode>();
        var duplicatedStarts = new List<EntryData>();
        foreach (var entry in entries)
        {
            if (entry.ScopeId == -1)
                endEntries.Add(entry);
            else if (entry.ScopeId == entry.ParentScopeId)
                normalEntries.Add(entry);
            else if (!scopeNodesDict.TryAdd(entry.ScopeId, new LogScopeNode(entry))) 
                duplicatedStarts.Add(entry);
        }
        // build tree
        var rootScopeId = -1L;
        foreach (var (scopeId, n) in scopeNodesDict)
        {
            var e = n.StartEntry;
            if (scopeNodesDict.TryGetValue(e.ParentScopeId, out var parent))
                parent.ChildrenInternal.Add(new LogScopeChild.Scope(n));
            else
            {
                if (rootScopeId != -1L && rootScopeId != e.ParentScopeId) 
                    throw new Exception("Multiple root scopes");
                rootScopeId = e.ParentScopeId;
                rootChildren.Add(new LogScopeChild.Scope(n));
            }
        }
        // allocate children
        foreach (var entry in normalEntries)
        {
            if (scopeNodesDict.TryGetValue(entry.ScopeId, out var n)) 
                n.ChildrenInternal.Add(new LogScopeChild.Line(entry));
            else
            {
                if (rootScopeId != -1L && rootScopeId != entry.ScopeId)
                    throw new Exception("Multiple root scopes");
                rootScopeId = entry.ScopeId;
                rootChildren.Add(new LogScopeChild.Line(entry));
            }
        }
        // assign end entries
        var orphanEnds = new List<EntryData>();
        foreach (var entry in endEntries)
        {
            if (scopeNodesDict.TryGetValue(entry.ParentScopeId, out var n))
                n.EndEntry = entry;
            else
                orphanEnds.Add(entry);
        }
        // sort children of nodes
        foreach (var n in scopeNodesDict.Values)
            SortChildren(n.ChildrenInternal);
        SortChildren(rootChildren);

        TotalStats = CountStats(null, rootChildren);
        Children = [.. rootChildren];
        OrphanEnds = [.. orphanEnds];
        DuplicatedStarts = [.. duplicatedStarts];
    }

    private static EntryStats CountStats(LogScopeNode? scopeNode, IReadOnlyList<LogScopeChild> children)
    {
        int infos = 0, subInfos = 0, errors = 0, warnings = 0;
        var childNodeStats = EntryStats.Zero;
        foreach (var c in children)
        {
            switch (c)
            {
                case LogScopeChild.Scope s:
                    s.Node.TotalStats = CountStats(s.Node, s.Node.Children);
                    childNodeStats += s.Node.TotalStats;
                    break;
                case LogScopeChild.Line l:
                    switch (l.Entry.Type)
                    {
                        case EntryType.Info: infos++; break;
                        case EntryType.SubInfo: subInfos++; break;
                        case EntryType.Error: errors++; break;
                        case EntryType.Warning: warnings++; break;
                    }
                    break;
            }
        }
        var localStats = new EntryStats { Infos = infos, SubInfos = subInfos, Errors = errors, Warnings = warnings };
        scopeNode?.LocalStats = localStats;
        return localStats + childNodeStats;
    }
    private static void SortChildren(List<LogScopeChild> children)
    {
        // Cluster siblings by FlowKey then ThreadId; clusters ordered by their first entry's
        // timestamp, entries within each (FlowKey, ThreadId) cluster ordered by timestamp.
        var list = new List<LogScopeChild>();
        var childFlowGroups = children.GroupBy(static c => c.GetEntryData().FlowKey)
            .OrderBy(static cs => cs.First().GetEntryData().Timestamp);
        foreach (var childFlowGroup in childFlowGroups)
        {
            var childFlowGroupThreadGroups = childFlowGroup.OrderBy(static c => c.GetEntryData().Timestamp)
                .GroupBy(static c => c.GetEntryData().ThreadId)
                .OrderBy(static cs => cs.First().GetEntryData().Timestamp);
            list.AddRange(childFlowGroupThreadGroups.SelectMany(cs => cs));
        }
        children.ReplaceAll(list);
    }
}
