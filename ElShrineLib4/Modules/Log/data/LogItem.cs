namespace ElShrine.Modules.Log;

public record LogItem(string Text, LogItemStyle Style, LogItemFormat Format)
{
    public override string ToString()
    {
        return Format switch
        {
            LogItemFormat.Normal => Text,
            LogItemFormat.Brackets => $"[{Text}]",
            LogItemFormat.Parentheses => $"({Text})",
            LogItemFormat.Braces => $"{{{Text}}}",
            LogItemFormat.Quotations => $"\"{Text}\"",
            LogItemFormat.AngleBrackets => $"<{Text}>",
            _ => Text
        };
    }

    #region operator +
    public static implicit operator LogItem(string text)
        => text.Info();
    public static LogItem operator +(LogItem left, string right)
        => new(left.Text + right, left.Style, left.Format);
    public static LogItem operator +(string left, LogItem right)
        => new(left + right.Text, right.Style, right.Format);
    #endregion
}
