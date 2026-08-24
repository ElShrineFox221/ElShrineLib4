namespace ElShrine.Modules.Log;

public interface ILogSessionFactory
{
    IEnumerable<string> GetSessions();
    LogSession GetOrCreateSession(string sessionName);
}
