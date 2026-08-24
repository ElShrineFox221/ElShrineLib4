using System.Diagnostics;
using System.Globalization;

namespace ElShrine.Modules.Log;

/// <summary>
/// Unique identity of a single log run (process start→stop boundary).
/// Raw value: UTC .NET ticks. String form: <c>Log[yyyy-MM-dd_HH-mm-ss_fff]#HHHHHHHHHHHHHHHH</c>
/// where the hex suffix carries the identity and the bracket portion is human-readable local time.
/// </summary>
public readonly struct LogRunId(long raw) : IComparable<LogRunId>, IComparable, IEquatable<LogRunId>
{
    private const string PREFIX = "Log[";
    private const string SUFFIX = "]";
    private const string HEX_SEPARATOR = "#";
    private const string LOCAL_FORMAT = "yyyy-MM-dd_HH-mm-ss_fff";
    private const int HEX_CODE_LENGTH = 16;

    public static readonly LogRunId Current;

    static LogRunId()
    {
        long raw;
        try
        {
            raw = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
        }
        catch
        {
            raw = DateTime.UtcNow.Ticks;
        }
        Current = new LogRunId(raw);
    }

    public long Raw { get; } = raw;

    public DateTime UtcTime => new(Raw, DateTimeKind.Utc);

    public DateTime LocalTime => UtcTime.ToLocalTime();

    /// <summary>
    /// Parses a run folder name. Only the 16-char hex suffix after the last <c>#</c> is
    /// interpreted; the human-readable prefix is intentionally not validated.
    /// </summary>
    public static bool TryParse(string folderName, out LogRunId id)
    {
        id = default;
        if (string.IsNullOrWhiteSpace(folderName))
            return false;

        var hexSepIdx = folderName.LastIndexOf(HEX_SEPARATOR, StringComparison.Ordinal);
        if (hexSepIdx < 0)
            return false;

        var hex = folderName.AsSpan(hexSepIdx + 1);
        if (hex.Length != HEX_CODE_LENGTH)
            return false;
        if (!long.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var raw))
            return false;

        id = new LogRunId(raw);
        return true;
    }

    public static LogRunId Parse(string folderName)
    {
        if (TryParse(folderName, out var id))
            return id;
        throw new FormatException(
            $"Folder name '{folderName}' is not a valid log run folder. " +
            $"Expected a 16-char uppercase hex suffix after '{HEX_SEPARATOR}'.");
    }

    public int CompareTo(LogRunId other) => Raw.CompareTo(other.Raw);
    int IComparable.CompareTo(object? obj) => obj is LogRunId other ? CompareTo(other) : 0;
    public bool Equals(LogRunId other) => Raw == other.Raw;
    public override bool Equals(object? obj) => obj is LogRunId other && Equals(other);
    public override int GetHashCode() => Raw.GetHashCode();
    public static bool operator ==(LogRunId left, LogRunId right) => left.Equals(right);
    public static bool operator !=(LogRunId left, LogRunId right) => !left.Equals(right);
    public static bool operator <(LogRunId left, LogRunId right) => left.CompareTo(right) < 0;
    public static bool operator >(LogRunId left, LogRunId right) => left.CompareTo(right) > 0;

    public override string ToString()
    {
        var localPart = LocalTime.ToString(LOCAL_FORMAT, CultureInfo.InvariantCulture);
        var hexPart = Raw.ToString("X16", CultureInfo.InvariantCulture);
        return $"{PREFIX}{localPart}{SUFFIX}{HEX_SEPARATOR}{hexPart}";
    }
}
