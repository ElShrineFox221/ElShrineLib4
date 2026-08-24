namespace ElShrine.Modules.Log;

public readonly record struct EntryContentBuilderOptions
{
    public readonly string IndentUnit;

    public EntryContentBuilderOptions(string indentUnit)
    {
        if (indentUnit.Contains('\n'))
            throw new ArgumentException($"{nameof(IndentUnit)} must be in one line.");
        IndentUnit = indentUnit;
    }

    public static readonly EntryContentBuilderOptions Default = new("  ");
}
