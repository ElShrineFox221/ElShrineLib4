namespace ElShrine.Modules.Log;

public static class LogItemExtensions
{
    #region styles

    #region from item
    public static LogItem WithStyle(this LogItem item, LogItemStyle style)
        => item with { Style = style };
    // logs
    public static LogItem Info(this LogItem item) => item.WithStyle(LogItemStyle.Info);
    public static LogItem SubInfo(this LogItem item) => item.WithStyle(LogItemStyle.SubInfo);
    public static LogItem Success(this LogItem item) => item.WithStyle(LogItemStyle.Success);
    public static LogItem Error(this LogItem item) => item.WithStyle(LogItemStyle.Error);
    public static LogItem Warning(this LogItem item) => item.WithStyle(LogItemStyle.Warning);
    // code styles
    public static LogItem Type(this LogItem item) => item.WithStyle(LogItemStyle.Type);
    public static LogItem Member(this LogItem item) => item.WithStyle(LogItemStyle.Member);
    public static LogItem Parameter(this LogItem item) => item.WithStyle(LogItemStyle.Parameter);
    public static LogItem Method(this LogItem item) => item.WithStyle(LogItemStyle.Method);
    public static LogItem Annotation(this LogItem item) => item.WithStyle(LogItemStyle.Annotation);
    #endregion

    #region from string
    public static LogItem WithStyle(this string text, LogItemStyle style)
        => new(text, style, LogItemFormat.Normal);
    // logs
    public static LogItem Info(this string text) => text.WithStyle(LogItemStyle.Info);
    public static LogItem SubInfo(this string text) => text.WithStyle(LogItemStyle.SubInfo);
    public static LogItem Success(this string text) => text.WithStyle(LogItemStyle.Success);
    public static LogItem Error(this string text) => text.WithStyle(LogItemStyle.Error);
    public static LogItem Warning(this string text) => text.WithStyle(LogItemStyle.Warning);
    // code styles
    public static LogItem Type(this string text) => text.WithStyle(LogItemStyle.Type);
    public static LogItem Member(this string text) => text.WithStyle(LogItemStyle.Member);
    public static LogItem Parameter(this string text) => text.WithStyle(LogItemStyle.Parameter);
    public static LogItem Method(this string text) => text.WithStyle(LogItemStyle.Method);
    public static LogItem Annotation(this string text) => text.WithStyle(LogItemStyle.Annotation);
    #endregion

    #endregion

    #region formats

    #region for item
    public static LogItem WithFormat(this LogItem item, LogItemFormat format)
        => item with { Format = format };
    //
    public static LogItem Normal(this LogItem item) => item.WithFormat(LogItemFormat.Normal);
    public static LogItem Parentheses(this LogItem item) => item.WithFormat(LogItemFormat.Parentheses);
    public static LogItem Brackets(this LogItem item) => item.WithFormat(LogItemFormat.Brackets);
    public static LogItem Braces(this LogItem item) => item.WithFormat(LogItemFormat.Braces);
    public static LogItem AngleBrackets(this LogItem item) => item.WithFormat(LogItemFormat.AngleBrackets);
    public static LogItem Quotations(this LogItem item) => item.WithFormat(LogItemFormat.Quotations);
    #endregion

    #region from string
    public static LogItem WithFormat(this string item, LogItemFormat format)
        => new(item, LogItemStyle.Info, format);
    //
    public static LogItem Normal(this string item) => item.WithFormat(LogItemFormat.Normal);
    public static LogItem Parentheses(this string item) => item.WithFormat(LogItemFormat.Parentheses);
    public static LogItem Brackets(this string item) => item.WithFormat(LogItemFormat.Brackets);
    public static LogItem Braces(this string item) => item.WithFormat(LogItemFormat.Braces);
    public static LogItem AngleBrackets(this string item) => item.WithFormat(LogItemFormat.AngleBrackets);
    public static LogItem Quotations(this string item) => item.WithFormat(LogItemFormat.Quotations);
    #endregion

    #endregion
}