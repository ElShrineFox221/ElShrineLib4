namespace ElShrine.DependencyInjection;

public enum UnregisteredPolicy
{
    Inherit,
    Reject,
    Construct
}

public readonly record struct ScopeOptions
{
    public UnregisteredPolicy Unregistered { get; init; }
}
