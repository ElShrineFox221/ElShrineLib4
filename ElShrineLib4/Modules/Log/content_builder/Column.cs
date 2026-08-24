namespace ElShrine.Modules.Log;

public readonly record struct Column
{
    public LogItem[][] Rows { get; }
    public CellOptions[] RowOptions { get; }
    public ColumnLayoutOptions Layout { get; }

    private Column(LogItem[][] rows, CellOptions[] rowOptions, ColumnLayoutOptions layout)
    {
        Rows = rows;
        RowOptions = rowOptions;
        Layout = layout;
    }

    #region default selectors
    // 与 EntryContentBuilder.AddColumnInternal 的默认 selector 保持一致
    private static LogItem[] DefaultItemSelector<T>(T item, int idx) => item switch
    {
        LogItem li => [li],
        string s  => [new LogItem(s, LogItemStyle.Info, LogItemFormat.Normal)],
        _         => [new LogItem(item?.ToString() ?? string.Empty, LogItemStyle.Info, LogItemFormat.Normal)],
    };
    private static CellOptions? DefaultOptionsSelector<T>(T item, int idx) => item switch
    {
        CellOptions os => os,
        _              => null,
    };
    #endregion

    #region Of
    /// <summary>
    /// Creates a column from <paramref name="items"/>. The default item selector converts
    /// <see cref="LogItem"/> and <see cref="string"/> elements; the default options selector
    /// picks up <see cref="CellOptions"/> elements.
    /// </summary>
    /// <param name="defaultOptions">Fallback <see cref="CellOptions"/> when an item yields none; defaults to <see cref="CellOptions.Default"/> when null.</param>
    public static Column Of<T>(
        IEnumerable<T> items,
        Func<T, int, LogItem[]>? selector = null,
        Func<T, int, CellOptions?>? optionsSelector = null,
        CellOptions? defaultOptions = null,
        ColumnLayoutOptions layout = default)
    {
        selector ??= DefaultItemSelector<T>;
        optionsSelector ??= DefaultOptionsSelector<T>;
        var actualDefault = defaultOptions ?? CellOptions.Default;

        var itemsList = items.ToList();
        var rows = new LogItem[itemsList.Count][];
        var opts = new CellOptions[itemsList.Count];

        if (itemsList.Count > 0)
        {
            Parallel.ForEach(itemsList, (item, _, lIdx) =>
            {
                var idx = (int)lIdx;
                rows[idx] = selector(item, idx) ?? [];
                opts[idx] = optionsSelector(item, idx) ?? actualDefault;
            });
        }
        return new Column(rows, opts, layout);
    }
    #endregion
}
