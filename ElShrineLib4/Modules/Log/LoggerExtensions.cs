namespace ElShrine.Modules.Log;

public static class LoggerExtensions
{
    #region Info
    public static void Info(this ILogger logger, string text, string? flowKey = null)
        => logger.Log(EntryType.Info, text, flowKey);
    public static void Info(this ILogger logger, LogItem item, string? flowKey = null)
        => logger.Log(EntryType.Info, item, flowKey);
    public static void Info(this ILogger logger, params LogItem[] items)
        => logger.Log(EntryType.Info, new EntryContent(items));
    #endregion

    #region SubInfo
    public static void SubInfo(this ILogger logger, string text, string? flowKey = null)
        => logger.Log(EntryType.SubInfo, text, flowKey);
    public static void SubInfo(this ILogger logger, LogItem item, string? flowKey = null)
        => logger.Log(EntryType.SubInfo, item, flowKey);
    public static void SubInfo(this ILogger logger, params LogItem[] items)
        => logger.Log(EntryType.SubInfo, new EntryContent(items));
    #endregion

    #region Error
    public static void Error(this ILogger logger, string text, string? flowKey = null)
        => logger.Log(EntryType.Error, text, flowKey);
    public static void Error(this ILogger logger, LogItem item, string? flowKey = null)
        => logger.Log(EntryType.Error, item, flowKey);
    public static void Error(this ILogger logger, params LogItem[] items)
        => logger.Log(EntryType.Error, new EntryContent(items));
    #endregion

    #region Warning
    public static void Warning(this ILogger logger, string text, string? flowKey = null)
        => logger.Log(EntryType.Warning, text, flowKey);
    public static void Warning(this ILogger logger, LogItem item, string? flowKey = null)
        => logger.Log(EntryType.Warning, item, flowKey);
    public static void Warning(this ILogger logger, params LogItem[] items)
        => logger.Log(EntryType.Warning, new EntryContent(items));
    #endregion

    #region BeginScope
    public static LogScope BeginScope(this ILogger logger, params LogItem[] items)
        => logger.BeginScope(new EntryContent(items));
    #endregion
}
