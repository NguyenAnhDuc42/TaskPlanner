public static class FractionalIndex
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int AlphabetLength = 62;

    private static readonly int[] AlphabetIndex = BuildIndex();

    private static int[] BuildIndex()
    {
        var index = new int[128];
        Array.Fill(index, -1);
        for (int i = 0; i < AlphabetLength; i++)
            index[Alphabet[i]] = i;
        return index;
    }

    private static int GetIndex(char c) => c < 128 ? AlphabetIndex[c] : -1;

    public static string Between(string? before, string? after)
    {
        ReadOnlySpan<char> b = before.AsSpan();
        ReadOnlySpan<char> a = after.AsSpan();

        if (b.IsEmpty && a.IsEmpty) return "V";

        int length = Math.Max(b.Length, a.Length) + 1;
        var result = new System.Text.StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            char bc = i < b.Length ? b[i] : '\0';
            char ac = i < a.Length ? a[i] : (char)127;

            if (bc == ac) { result.Append(bc); continue; }

            int bIdx = GetIndex(bc);           // -1 if below alphabet floor
            int aIdx = GetIndex(ac);
            if (aIdx == -1) aIdx = AlphabetLength; // above alphabet ceiling

            int midIdx = (Math.Max(bIdx, 0) + aIdx) / 2;

            if (midIdx > bIdx)
            {
                result.Append(Alphabet[midIdx]);
                break;
            }

            result.Append(bc);
            if (i >= b.Length - 1)
            {
                result.Append(Alphabet[AlphabetLength / 2]);
                break;
            }
        }

        return result.ToString();
    }

    public static string Start()             => "V";
    public static string After(string key)  => Between(key, null);
    public static string Before(string key) => Between(null, key);
}