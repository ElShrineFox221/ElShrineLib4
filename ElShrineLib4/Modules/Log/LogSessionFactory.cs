using System.Collections.Concurrent;

namespace ElShrine.Modules.Log;

public class LogSessionFactory(ILogWriter writer) : ILogSessionFactory
{
    private readonly ILogWriter _writer = writer;
    private readonly ConcurrentDictionary<string, LogSession> _sessions = [];

    public IEnumerable<string> GetSessions() => _sessions.Keys;
    public LogSession GetOrCreateSession(string sessionName)
        => _sessions.GetOrAdd(sessionName, n => new LogSession(_writer, n));
}
