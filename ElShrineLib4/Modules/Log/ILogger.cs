namespace ElShrine.Modules.Log;

public interface ILogger
{
    LogScope BeginScope(EntryContent content, string? flowKey = null);
    void Log(EntryType type, EntryContent content, string? flowKey = null);
}
