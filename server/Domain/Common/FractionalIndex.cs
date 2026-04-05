namespace Domain.Common;

public static class FractionalIndex
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Between(string? before, string? after)
    {
        before ??= "0";
        after  ??= "z";

        if (string.Compare(before, after, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"'before' must be less than 'after'. Got: before='{before}', after='{after}'");

        var result = new System.Text.StringBuilder();
        int i = 0;
        string b = before;
        string a = after;

        while (true)
        {
            char bc = i < b.Length ? b[i] : '0';
            char ac = i < a.Length ? a[i] : 'z';

            if (bc == ac)
            {
                result.Append(bc);
                i++;
                continue;
            }

            int bIndex = Alphabet.IndexOf(bc);
            int aIndex = Alphabet.IndexOf(ac);
            int mid = (bIndex + aIndex) / 2;

            if (mid == bIndex)
            {
                // No room at this character level — go one level deeper
                result.Append(bc);
                b = i + 1 < b.Length ? b[(i + 1)..] : "0";
                a = "z";
                i = 0;
                continue;
            }

            result.Append(Alphabet[mid]);
            break;
        }

        return result.ToString();
    }

    /// <summary>Default starting key for the first item ("V" = midpoint of alphabet).</summary>
    public static string Start() => "V";

    /// <summary>Generate a key that sorts after <paramref name="key"/>.</summary>
    public static string After(string key) => Between(key, null);

    /// <summary>Generate a key that sorts before <paramref name="key"/>.</summary>
    public static string Before(string key) => Between(null, key);
}
