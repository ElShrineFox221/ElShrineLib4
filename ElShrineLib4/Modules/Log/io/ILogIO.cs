namespace ElShrine.Modules.Log;

public interface ILogIO
{
    string BaseDir { get; }
    string CurrentDir => Path.Combine(BaseDir, LogRunId.Current.ToString());
}
