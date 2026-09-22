namespace ElShrine.DependencyInjection;

internal sealed class ResolutionContext(Container origin)
{
    public Container Origin { get; set; } = origin;
    public Dictionary<Type, object> TemporaryCache { get; } = [];
    public Stack<Type> ResolvingStack { get; } = new();

    public IReadOnlyList<Type> BuildCyclePath(Type repeated)
    {
        var chain = ResolvingStack.Reverse().ToList();
        var start = chain.IndexOf(repeated);
        var cycle = start > 0 ? chain.GetRange(start, chain.Count - start) : chain;
        cycle.Add(repeated);
        return cycle;
    }
}
