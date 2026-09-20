using System.Diagnostics;

namespace ElShrine;

public static class CommonExtensions
{
    #region array, list, collection, etc
    public static bool DictionaryEqual<TK, TV>(this IReadOnlyDictionary<TK, TV> a, IReadOnlyDictionary<TK, TV> b)
    {
        if (a.Count != b.Count)
            return false;
        foreach (var item in a)
        {
            if (b.TryGetValue(item.Key, out var value) && Equals(item.Value, value))
                continue;
            return false;
        }
        return true;
    }
    public static T[] Slice<T>(this IEnumerable<T> values, int startIndex, int endIndex)
        => [.. values.Skip(startIndex).Take(endIndex - startIndex + 1)];

    public static void ReplaceAll<T>(this IList<T> list, params IEnumerable<T> values)
    {
        list.Clear();
        if (list is List<T> sysList) 
            sysList.AddRange(values);
        else
        {
            foreach (var value in values) 
                list.Add(value);
        }
    }
    public static T GetOrCreate<T>(this IList<T> list, int index, Func<T> instantiate, bool modifySource = false)
    {
        T result;

        if (index >= list.Count || index < 0) 
            result = instantiate();
        else 
            result = list[index];

        if (modifySource) 
            list.Add(result);
        return result;
    }
    public static T Clamped<T>(this IList<T> list, int index)
    {
        var clampedIndex = Math.Min(Math.Max(index, 0), list.Count - 1);
        return list[clampedIndex];
    }
    #endregion

    #region string & char

    #region pural
    public static string GetPural(this string str, int num)
        => str.GetPural(int.Abs(num) > 1);
    public static string GetPural(this string str, bool isPural)
        => !isPural ? str :
        str.EndsWith('s') || str.EndsWith('x') || str.EndsWith('z') ||
        str.EndsWith("sh") || str.EndsWith("ch") ? str + "es" :
        str.EndsWith('y') && str.Length > 1 && !"aeiou".Contains(str[^2]) ?
        str[..^1] + "ies" : str + "s";
    public static string GetPuralWithNum(this string str, int num)
        => $"{num} {str.GetPural(num)}";
    #endregion

    #region width calculation
    public static bool IsFullChar(this char c)
    {
        // �ո�(0x20) + Ӣ�� / ���� / Ӣ�ı��(0x21 ~0x7E)
        if (c >= 0x20 && c <= 0x7E) return false;
        // ȫ�ǿո�
        if (c == 0x3000) return true;
        // ȫ��ASCII�ַ�
        if (c >= 0xFF01 && c <= 0xFF5E) return true;
        // ȫ�Ǳ�����
        if (c >= 0x3000 && c <= 0x303F) return true;
        // ƽ������Ƭ����(JP)
        if (c >= 0x3040 && c <= 0x30FF) return true;
        // CJK ͳһ��������
        if (c >= 0x4E00 && c <= 0x9FFF) return true;
        // CJK ��չ A
        if (c >= 0x3400 && c <= 0x4DBF) return true;
        // CJK ���ݺ���
        if (c >= 0xF900 && c <= 0xFAFF) return true;
        // ��������
        if (c >= 0xAC00 && c <= 0xD7AF) return true;
        return false;
    }
    public static int CalcTextWidth(this string text)
    {
        var width = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsControl(c))
                continue;
            width += IsFullChar(c) ? 2 : 1;
        }
        return width;
    }
    #endregion

    #endregion

    #region stopwatch
    public static string GetStopwatchElapsed(this Stopwatch sw, bool restartStopwatch = false)
    {
        sw.Stop();
        var r = $"{sw.ElapsedMilliseconds} ms consumed";
        if (restartStopwatch)
            sw.Restart();
        else
            sw.Start();
        return r;
    }
    #endregion
}
