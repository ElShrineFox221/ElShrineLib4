using System.Text;

namespace ElShrine.Modules.Log;

public sealed record EntryContent
{
    public static readonly EntryContent Empty = new([]);

    public EntryContent(LogItem[] items)
        => _items = items;

    private readonly LogItem[] _items;

    public LogItem[] Items => _items;

    #region operator + & implicit
    public static EntryContent operator +(EntryContent left, EntryContent right)
        => new([.. left._items, .. right._items]);
    public static EntryContent operator +(EntryContent left, LogItem right)
        => new([.. left._items, right]);
    public static EntryContent operator +(LogItem left, EntryContent right)
        => new([left, .. right._items]);
    public static implicit operator EntryContent(LogItem[] items)
        => new(items);
    public static implicit operator EntryContent(LogItem item)
        => new([item]);
    public static implicit operator EntryContent(string text)
        => new([new LogItem(text, LogItemStyle.Info, LogItemFormat.Normal)]);
    #endregion

    public override string ToString()
    {
        if (_items.Length == 0) 
            return string.Empty;
        var sbuilder = new StringBuilder();
        foreach (var item in _items) 
            sbuilder.Append(item.ToString());
        return sbuilder.ToString();
    }
}
