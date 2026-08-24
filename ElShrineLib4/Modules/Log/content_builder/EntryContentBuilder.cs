using ColumnAlignment = ElShrine.Modules.Log.CellOptions.ColumnAlignment;

namespace ElShrine.Modules.Log;

public class EntryContentBuilder
{
    #region nested types
    private enum LineModelKind { Line, Separator, Columns }
    private enum AnnotationSide { Left, Right, Full }
    private readonly record struct ToReplaceItemInfo(int ItemIndex, AnnotationSide Side, int InfoWidth, char Replace);
    private readonly record struct ColumnRow(LogItem[] Content, CellOptions Options);
    private readonly record struct ColumnData(ColumnRow[] Rows, ColumnLayoutOptions Layout);
    private readonly record struct LineModel
    {
        public LineModelKind Kind { get; }
        public int IndentLevel { get; }
        public LogItem[] LineItems { get; }
        public char SeparatorFill { get; }
        public LogItemStyle SeparatorStyle { get; }
        public ColumnData[] Columns { get; }
        public int ColumnGap { get; }

        private LineModel(
            LineModelKind kind, int indentLevel,
            LogItem[] lineItems,
            char separatorFill, LogItemStyle separatorStyle,
            ColumnData[] columns, int columnGap)
        {
            Kind = kind;
            IndentLevel = indentLevel;
            LineItems = lineItems;
            SeparatorFill = separatorFill;
            SeparatorStyle = separatorStyle;
            Columns = columns;
            ColumnGap = columnGap;
        }

        public static LineModel CreateLine(LogItem[] items, int indentLevel)
            => new(LineModelKind.Line, indentLevel, items, default, default, [], 0);

        public static LineModel CreateSeparator(LogItem[] title, char fill, LogItemStyle style, int indentLevel)
            => new(LineModelKind.Separator, indentLevel, title, fill, style, [], 0);

        public static LineModel CreateColumns(ColumnData[] columns, int columnGap, int indentLevel)
            => new(LineModelKind.Columns, indentLevel, [], default, default, columns, columnGap);
    }
    private readonly record struct ActualLine(LogItem[] Items, int Width);
    #endregion

    private const int SeparatorFillSideMinimumCharsNum = 5;
    private readonly List<LineModel> _models;
    private int _indentLevel;

    #region line model buffers
    private LineModelKind? _currentKind;
    private readonly List<LogItem> _currentLineItems = [];
    private readonly List<ColumnData> _currentColumns = [];
    private int _currentColumnGap;
    private (char Fill, LogItemStyle Style) _separatorBuffer;
    #endregion

    public int IndentLevel => _indentLevel;
    public EntryContentBuilderOptions Options
    {
        get => field;
        set
        {
            if (field != value)
            {
                var oldValue = field;
                field = value;
                if (oldValue.IndentUnit != value.IndentUnit || oldValue.IndentUnit.CalcTextWidth() != value.IndentUnit.CalcTextWidth())
                    _cachedIndentItems.Clear();
            }
        }
    } = EntryContentBuilderOptions.Default;

    // Constructors
    public EntryContentBuilder() => _models = [];
    public EntryContentBuilder(int initialCapacity) => _models = new(initialCapacity);

    #region Core
    private void CloseLineModel()
    {
        if (_currentKind == null) 
            return;

        switch (_currentKind.Value)
        {
            case LineModelKind.Line:
                _models.Add(LineModel.CreateLine([.. _currentLineItems], _indentLevel));
                _currentLineItems.Clear();
                break;
            case LineModelKind.Separator:
                _models.Add(LineModel.CreateSeparator(
                    [.._currentLineItems], _separatorBuffer.Fill, _separatorBuffer.Style, _indentLevel));
                _separatorBuffer = default;
                _currentLineItems.Clear();
                break;
            case LineModelKind.Columns:
                if (_currentColumns.Count > 0)
                    _models.Add(LineModel.CreateColumns([.. _currentColumns], _currentColumnGap, _indentLevel));
                _currentColumns.Clear();
                break;
        }
        _currentKind = null;
    }
    private void EnsureLineModel(LineModelKind kind)
    {
        if (_currentKind == kind) 
            return;
        if (_currentKind != null)
            CloseLineModel();
        _currentKind = kind;
    }
    private void AppendInternal(LogItem item)
    {
        EnsureLineModel(LineModelKind.Line);
        _currentLineItems.Add(item);
    }
    private void SeparatorInternal(LogItem[] title, char fill, LogItemStyle fillCharStyle)
    {
        EnsureLineModel(LineModelKind.Separator);
        _currentLineItems.AddRange(title);
        _separatorBuffer = (fill, fillCharStyle);
        CloseLineModel();
    }
    private void AddColumnInternal<T>(IEnumerable<T> items, Func<T, int, LogItem[]>? rowSelector, int columnGap, Func<T, int, CellOptions?>? rowOptionsSelector, CellOptions defaultOptions, ColumnLayoutOptions? columnLayout)
    {
        EnsureLineModel(LineModelKind.Columns);
        _currentColumnGap = columnGap;

        rowSelector ??= static (i, idx) => i switch
        {
            LogItem li => [li],
            string s => [new LogItem(s, LogItemStyle.Info, LogItemFormat.Normal)],
            _ => [new LogItem(i?.ToString() ?? "", LogItemStyle.Info, LogItemFormat.Normal)]
        };
        rowOptionsSelector ??= static (i, idx) => i switch
        {
            CellOptions os => os,
            _ => null
        };

        var layout = columnLayout ?? default;
        var itemsList = items.ToList();
        var rows = new ColumnRow[itemsList.Count];
        Parallel.ForEach(itemsList, (i, _, lIdx) =>
        {
            var idx = (int)lIdx;
            var lItems = rowSelector.Invoke(i, idx);
            var lOptions = rowOptionsSelector.Invoke(i, idx) ?? defaultOptions;
            var row = new ColumnRow(lItems, lOptions);
            rows[idx] = row;
        });

        _currentColumns.Add(new(rows, layout));
    }
    #endregion

    #region Result

    #region Convert
    private readonly Dictionary<int, int> _modelWidths = [];
    private readonly Dictionary<int, List<ToReplaceItemInfo>> _separatorAlignItemInfos = [];
    public LogItem[] ToArray()
    {
        CloseLineModel();

        var result = new List<LogItem>();
        for (int i = 0; i < _models.Count; i++)
        {
            var model = _models[i];
            switch (model.Kind)
            {
                case LineModelKind.Line:
                    _modelWidths[i] = ConstructLine(model, result);
                    break;
                case LineModelKind.Separator:
                    _modelWidths[i] = -1;
                    _separatorAlignItemInfos[i] = PreConstructSeparator(model, result);
                    break;
                case LineModelKind.Columns:
                    _modelWidths[i] = ConstructColumns(model, result);
                    break;
            }
        }
        const int SeparatorMinimumWidth = 40;
        var targetWidth = SeparatorMinimumWidth;
        for (var i = _models.Count - 1; i >= 0; i--)
        {
            var w = _modelWidths[i];
            if (w >= 0) 
                targetWidth = Math.Max(targetWidth, w);
            else
            {
                ConstructSeparatorAlignItmes(_separatorAlignItemInfos[i], targetWidth, result);
                targetWidth = SeparatorMinimumWidth;
            }
        }
        _modelWidths.Clear();
        _separatorAlignItemInfos.Clear();

        return [.. result];
    }
    public EntryContent ToEntryContent() => new(ToArray());

    public static implicit operator EntryContent(EntryContentBuilder builder) => builder.ToEntryContent();
    #endregion

    #region construction
    private int ConstructLine(LineModel model, List<LogItem> result)
    {
        var items = model.LineItems;
        var indentItem = GetIndentItem(model.IndentLevel);
        var als = OrganizeLines([indentItem, .. items]);
        for (var i = 0; i < als.Length; i++)  
        {
            result.AddRange(als[i].Items);
            result.Add(_wrapItem);
        }
        return als.Max(static al => al.Width);
    }
    private List<ToReplaceItemInfo> PreConstructSeparator(LineModel model, List<LogItem> result)
    {
        var indentItem = GetIndentItem(model.IndentLevel);
        var indentWidth = indentItem.Text.CalcTextWidth();
        var emptyItem = new LogItem(string.Empty, model.SeparatorStyle, LogItemFormat.Normal);
        var als = OrganizeLines(model.LineItems);

        var infos = new List<ToReplaceItemInfo>();
        foreach (var al in als)
        {
            result.Add(indentItem);
            result.Add(emptyItem);
            if (al.Items.Length == 0)
                infos.Add(new(result.Count - 1, AnnotationSide.Full, indentWidth, model.SeparatorFill));
            else
            {
                infos.Add(new(result.Count - 1, AnnotationSide.Left, al.Width + indentWidth, model.SeparatorFill));
                result.AddRange(al.Items);
                result.Add(emptyItem);
                infos.Add(new(result.Count - 1, AnnotationSide.Right, al.Width + indentWidth, model.SeparatorFill));
            }
            result.Add(_wrapItem);
        }
        return infos;
    }
    private static void ConstructSeparatorAlignItmes(List<ToReplaceItemInfo> infos, int targetWidth, List<LogItem> result)
    {
        foreach (var info in infos)
        {
            var sub = targetWidth - info.InfoWidth;
            string fill = info.Side switch
            {
                AnnotationSide.Left => $"{new string(info.Replace, Math.Max(SeparatorFillSideMinimumCharsNum, sub / 2) - 1)} ",
                AnnotationSide.Right => $" {new string(info.Replace, Math.Max(SeparatorFillSideMinimumCharsNum, sub - sub / 2) - 1)}",
                _ => new string(info.Replace, Math.Max(SeparatorFillSideMinimumCharsNum * 2, sub)),
            };
            var item = new LogItem(fill, result[info.ItemIndex].Style, LogItemFormat.Normal);
            result[info.ItemIndex] = item;
        }
    }
    private int ConstructColumns(LineModel model, List<LogItem> result)
    {
        if (model.Columns.Length == 0)
            return 0;
        // calcs
        var cols = model.Columns;
        var colLines = new List<ActualLine[]>();
        var colMaxWidths = new int[cols.Length];
        for (var i = 0; i < cols.Length; i++)
        {
            var maxWidth = 0;
            var col = cols[i];
            colLines.Add([.. col.Rows.Select(r =>
            {
                var rw = r.Options.NewlineReplacement;
                var line = r.Content.Length == 0 ? new([], 0) : OrganizeLines([..r.Content.Select(i =>
                {
                    if (!i.Text.Contains('\n'))
                        return i;
                    return i with { Text = i.Text.Replace("\n", rw) };
                })])[0];
                maxWidth = Math.Max(maxWidth, line.Width);
                return line;
            })]);
            colMaxWidths[i] = maxWidth;
        }
        // render
        var resultWidth = 0;
        var maxRowsLength = colLines.Max(static ls => ls.Length);
        for (var i = 0; i < maxRowsLength; i++)
        {
            var inLineItems = colLines.SelectMany<ActualLine[], LogItem>((cals, cidx) =>
            {
                var empty = cals.Length <= i;
                var items = empty ? [] : cals[i].Items;
                var itemsWidth = empty ? 0 : cals[i].Width;
                var sub = colMaxWidths[cidx] - itemsWidth;

                var col = model.Columns[cidx];

                var isLeftPad = ColumnAlignment.Right == (col.Rows.Length > i ? col.Rows[i].Options.Alignment 
                    : (col.Rows.Length == 0 ? CellOptions.Default.Alignment : col.Rows[^1].Options.Alignment));
                var leftPad = col.Layout.LeftMargin + (isLeftPad ? sub : 0);
                var rightPad = col.Layout.RightMargin + (isLeftPad ? 0 : sub);
                if (cidx != colLines.Count - 1) 
                    rightPad += model.ColumnGap;
                if (leftPad != 0 && rightPad != 0)
                    return [GetEmptyItem(leftPad), .. items, GetEmptyItem(rightPad)];
                else if (leftPad != 0)
                    return [GetEmptyItem(leftPad), .. items];
                else if (rightPad != 0)
                    return [.. items, GetEmptyItem(rightPad)];
                else return [.. items];
            });
            var line = OrganizeLines([GetIndentItem(model.IndentLevel), .. inLineItems])[0];
            resultWidth = Math.Max(resultWidth, line.Width);
            if (line.Items.Length > 0)
                result.AddRange(line.Items);
            result.Add(_wrapItem);
        }
        static LogItem GetEmptyItem(int spaces) => new(new string(' ', spaces), LogItemStyle.Info, LogItemFormat.Normal);
        return resultWidth;
    }
    #endregion

    #region helpers
    private static readonly LogItem _wrapItem = new("\n", LogItemStyle.Info, LogItemFormat.Normal);
    private readonly Dictionary<int, LogItem> _cachedIndentItems = [];
    private LogItem GetIndentItem(int indentLevel)
    {
        if (!_cachedIndentItems.TryGetValue(indentLevel, out var item))
        {
            var indent = string.Empty;
            if (indentLevel > 0 && Options.IndentUnit.Length > 0)
            {
                var unit = Options.IndentUnit;
                var sb = new System.Text.StringBuilder(unit.Length * indentLevel);
                for (int l = 0; l < indentLevel; l++)
                    sb.Append(unit);
                indent = sb.ToString();
            }
            _cachedIndentItems[indentLevel] = item = new(indent, LogItemStyle.Info, LogItemFormat.Normal);
        }
        return item;
    }
    private static ActualLine[] OrganizeLines(LogItem[] items)
    {
        if (items.Length == 0)
            return [];
        var results = new List<ActualLine>();
        var list = new List<LogItem>();
        var width = 0;

        (LogItem? pre_item, bool pre_mergeable, bool pre_isWhiteText) = (null, false, false);
        foreach (var item in items)
            handleItem(item);
        endItems();

        void handleItem(LogItem item)
        {
            if(!item.Text.Contains('\n'))
            {
                if (pre_item is null)
                    pre_item = item;
                else if (pre_mergeable && item.Format is LogItemFormat.Normal)
                {
                    if (item.Style == pre_item.Style || pre_isWhiteText)
                        pre_item = item with { Text = pre_item.Text + item.Text };
                    else if (string.IsNullOrWhiteSpace(item.Text))
                        pre_item = pre_item with { Text = pre_item.Text + item.Text };
                    else
                    {
                        list.Add(pre_item);
                        width += pre_item.Text.CalcTextWidth();
                        pre_item = item;
                    }
                }
                else
                {
                    list.Add(pre_item);
                    width += pre_item.Text.CalcTextWidth();
                    pre_item = item;
                }
                pre_mergeable = pre_item.Format is LogItemFormat.Normal;
                pre_isWhiteText = string.IsNullOrWhiteSpace(pre_item.Text);
            }
            else
            {
                var sps = item.Text.Split('\n');
                for (var i = 0; i < sps.Length; i++)
                {
                    var spItem = new LogItem(sps[i], item.Style, item.Format);
                    handleItem(spItem);
                    if (i != sps.Length - 1)
                        endItems();
                }
            }
        }
        void endItems()
        {
            if (pre_item is not null)
            {
                list.Add(pre_item);
                width += pre_item.Text.CalcTextWidth();
            }
            results.Add(new([.. list.Where(i => i.Text.Length > 0 || i.Format != LogItemFormat.Normal)], width));
            width = 0;
            list.Clear();
            pre_item = null;
            pre_mergeable = false;
            pre_isWhiteText = false;
        }

        return [.. results];
    }
    #endregion

    #endregion

    #region Public operations

    #region Append

    public EntryContentBuilder Append(params LogItem[] items)
    {
        if (items.Length == 0)
            return this;
        foreach (var item in items)
            AppendInternal(item);
        return this;
    }

    public EntryContentBuilder Append(string text)
    {
        if (string.IsNullOrEmpty(text))
            return this;
        AppendInternal(new LogItem(text, LogItemStyle.Info, LogItemFormat.Normal));
        return this;
    }

    public EntryContentBuilder Append(LogItem item)
    {
        if (item is null)
            return this;
        AppendInternal(item);
        return this;
    }

    public EntryContentBuilder NewLine()
    {
        CloseLineModel();
        return this;
    }

    #endregion

    #region Separator

    // 占位空标题:PreConstructSeparator 中 OrganizeLines 过滤空 Text 后 al.Items.Length == 0,
    // 走 AnnotationSide.Full 填充路径,使纯填充 Separator 正常渲染整行。
    private static readonly LogItem _separatorEmptyTitle = new(string.Empty, LogItemStyle.Info, LogItemFormat.Normal);

    public EntryContentBuilder Separator(char fill)
    {
        SeparatorInternal([_separatorEmptyTitle], fill, LogItemStyle.Info);
        return this;
    }

    public EntryContentBuilder Separator(string title, char fill, LogItemStyle fillStyle = LogItemStyle.Info)
    {
        SeparatorInternal([new LogItem(title, LogItemStyle.Info, LogItemFormat.Normal)], fill, fillStyle);
        return this;
    }

    public EntryContentBuilder Separator(LogItem[] title, char fill, LogItemStyle fillStyle = LogItemStyle.Info)
    {
        SeparatorInternal(title, fill, fillStyle);
        return this;
    }

    #endregion

    #region Columns

    public EntryContentBuilder AddColumn<T>(
        IEnumerable<T> items,
        Func<T, int, LogItem[]>? selector = null,
        Func<T, int, CellOptions?>? optionsSelector = null,
        CellOptions? defaultOptions = null,
        ColumnLayoutOptions layout = default,
        int gap = 0)
    {
        var col = Column.Of(items, selector, optionsSelector, defaultOptions, layout);
        return AddColumns(gap, col);
    }

    public EntryContentBuilder AddColumns(int gap = 0, params Column[] columns)
    {
        EnsureLineModel(LineModelKind.Columns);
        _currentColumnGap = gap;

        foreach (var col in columns)
        {
            var rows = new ColumnRow[col.Rows.Length];
            for (var i = 0; i < col.Rows.Length; i++)
                rows[i] = new ColumnRow(col.Rows[i], col.RowOptions[i]);
            _currentColumns.Add(new ColumnData(rows, col.Layout));
        }
        return this;
    }

    #endregion

    #region Indent

    // 命令式语义(不引入 IDisposable using 块),返回 this 以保持链式。
    // 仅修改 _indentLevel,不触发封行;若需要在新缩进级别开始新行,应显式 NewLine。

    public EntryContentBuilder IndentTo(int level)
    {
        _indentLevel = Math.Max(0, level);
        return this;
    }
    public EntryContentBuilder Indent()
        => IndentTo(_indentLevel + 1);
    public EntryContentBuilder Unindent()
        => IndentTo(_indentLevel - 1);

    #endregion

    #endregion
}
