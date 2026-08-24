namespace ElShrine.Modules.Log;

/// <summary>
/// Per-<see cref="EntryType"/> tally. <see cref="Total"/> sums all four counts.
/// </summary>
public readonly record struct EntryStats
{
    public int Infos { get; init; }
    public int SubInfos { get; init; }
    public int Errors { get; init; }
    public int Warnings { get; init; }

    public int Total => Infos + SubInfos + Errors + Warnings;

    public static EntryStats Zero => default;

    public static EntryStats operator +(EntryStats a, EntryStats b) => new()
    {
        Infos = a.Infos + b.Infos,
        SubInfos = a.SubInfos + b.SubInfos,
        Errors = a.Errors + b.Errors,
        Warnings = a.Warnings + b.Warnings,
    };
}
