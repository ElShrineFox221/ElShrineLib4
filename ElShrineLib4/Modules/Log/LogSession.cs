namespace ElShrine.Modules.Log;

public class LogSession(ILogWriter writer, string sessionName) : ILogger
{
    public string SessionName { get; } = sessionName;
    public ILogWriter Writer { get; } = writer;
    public LogScope RootScope => _rootScope;

    private readonly LogScope _rootScope = LogScope.CreateRootScope(writer, sessionName);

    public void Log(EntryType type, EntryContent content, string? flowKey = null)
        => _rootScope.Log(type, content, flowKey);

    public LogScope BeginScope(EntryContent content, string? flowKey = null)
        => _rootScope.BeginScope(content, flowKey);
}
