namespace ElShrine.Modules.Log;

public static class LogItems
{
    public static readonly LogItem Success = new(nameof(Success), LogItemStyle.Success, LogItemFormat.Brackets);
    public static readonly LogItem Error = new(nameof(Error), LogItemStyle.Error, LogItemFormat.Brackets);
    public static readonly LogItem Warning = new(nameof(Warning), LogItemStyle.Warning, LogItemFormat.Brackets);
    public static readonly LogItem Info = new(nameof(Info), LogItemStyle.Info, LogItemFormat.Brackets);
    public static readonly LogItem SubInfo = new(nameof(SubInfo), LogItemStyle.SubInfo, LogItemFormat.Brackets);
    public static readonly LogItem Debug = new(nameof(Debug), LogItemStyle.Warning, LogItemFormat.Brackets);
    public static readonly LogItem Complete = new(nameof(Complete), LogItemStyle.Success, LogItemFormat.Brackets);
    public static readonly LogItem Cancelled = new(nameof(Cancelled), LogItemStyle.Warning, LogItemFormat.Brackets);
    public static readonly LogItem Failed = new(nameof(Failed), LogItemStyle.Error, LogItemFormat.Brackets);
}
