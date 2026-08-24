using ColumnAlignment = ElShrine.Modules.Log.CellOptions.ColumnAlignment;

namespace ElShrine.Modules.Log;

/// <summary>
/// Per-cell rendering options. <c>NewlineReplacement</c> is the string substituted for embedded newlines in the cell text.
/// </summary>
public readonly record struct CellOptions(ColumnAlignment Alignment = ColumnAlignment.Left, string NewlineReplacement = " ")
{
    public enum ColumnAlignment
    {
        Left,
        Right
    }

    public static readonly CellOptions Default = new();
    public static readonly CellOptions RightAligned = new(ColumnAlignment.Right);
}
