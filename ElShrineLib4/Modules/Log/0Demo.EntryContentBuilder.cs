namespace ElShrine.Modules.Log;

#if DEBUG
public static partial class LogDemo
{
    /// <summary>样例 1:基础 Append / NewLine</summary>
    public static void EntryContentBuilder_BasicUsage()
    {
        EntryContent content = new EntryContentBuilder()
            .Append("System initialized")
            .NewLine()
            .Append("Version ".SubInfo(), "1.0.0".Type())
            .NewLine()
            .Append("Build: ".SubInfo(), "2026-08-19".Annotation())
            .ToEntryContent();

        Console.WriteLine("--- 样例 1: Basic Append / NewLine ---");
        PrintContent(content);
    }

    /// <summary>样例 2:Separator 各种形态</summary>
    public static void EntryContentBuilder_SeparatorShowcase()
    {
        EntryContent content = new EntryContentBuilder()
            .Separator("Section A", '=')
            .Append("Content of section A")
            .NewLine()
            .Separator('-')
            .Append("After plain separator")
            .NewLine()
            .Separator("Warning", '!', LogItemStyle.Warning)
            .Append("Last line")
            .ToEntryContent();

        Console.WriteLine("--- 样例 2: Separator Showcase ---");
        PrintContent(content);
    }

    /// <summary>样例 3:列布局</summary>
    public static void EntryContentBuilder_ColumnsShowcase()
    {
        var modules = new[]
        {
            ("Core", "Loaded"),
            ("Audio", "Loaded"),
            ("Net", "Failed"),
        };

        EntryContent content = new EntryContentBuilder()
            .Separator("Modules", '=')
            .AddColumns(gap: 2,
                Column.Of(modules, (m, _) => [m.Item1.Type()]),
                Column.Of(modules, (m, _) => [m.Item2 == "Failed" ? m.Item2.Error() : m.Item2.Success()]))
            .Separator('=')
            .ToEntryContent();

        Console.WriteLine("--- 样例 3: Columns Showcase ---");
        PrintContent(content);
    }

    /// <summary>样例 4:Indent 缩进(命令式,需配合 NewLine)</summary>
    public static void EntryContentBuilder_IndentShowcase()
    {
        EntryContent content = new EntryContentBuilder()
            .Append("Root")
            .NewLine()
            .Indent()
            .Append("Level 1")
            .NewLine()
            .Indent()
            .Append("Level 2")
            .NewLine()
            .Unindent()
            .Append("Back to Level 1")
            .NewLine()
            .Unindent()
            .Append("Back to Root")
            .ToEntryContent();

        Console.WriteLine("--- 样例 4: Indent Showcase ---");
        PrintContent(content);
    }

    /// <summary>样例 5:混合布局(典型启动日志)</summary>
    public static void EntryContentBuilder_MixedLayout()
    {
        EntryContent content = new EntryContentBuilder()
            .Separator("System Startup", '=')
            .Append("Version ".SubInfo(), "1.0.0".Type())
            .NewLine()
            .Append("Time ".SubInfo(), "2026-08-19T10:00:00Z".Annotation())
            .NewLine()
            .Separator('-')
            .Append("Modules".Info())
            .NewLine()
            .Indent()
            .AddColumn(
                ["Core", "Audio", "Network"],
                selector: (m, _) => [m.Type(), " ".Normal(), m == "Network" ? "Failed".Error() : "OK".Success()],
                gap: 2)
            .Unindent()
            .Separator('=')
            .Append("Startup ".SubInfo(), "complete".Success())
            .ToEntryContent();

        Console.WriteLine("--- 样例 5: Mixed Layout ---");
        PrintContent(content);
    }

    private static void PrintContent(EntryContent content)
    {
        foreach (var item in content.Items)
            Console.Write(item.ToString());
        Console.WriteLine();
        Console.WriteLine();
    }
}
#endif
