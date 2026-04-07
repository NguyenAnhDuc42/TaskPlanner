namespace Domain.Common;

public static class FractionalIndex
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Between(string? before, string? after)
    {
        string b = before ?? "   "; // Default low (using space as base)
        string a = after  ?? "zzz"; // Default high

        if (string.Compare(b, a, StringComparison.Ordinal) >= 0)
        {
            // Fallback for inverted or equal keys to prevent crash
            return After(b);
        }

        var result = new System.Text.StringBuilder();
        int n = Math.Max(b.Length, a.Length) + 1; // Allow for one extra depth level if needed

        for (int i = 0; i < n; i++)
        {
            char bc = i < b.Length ? b[i] : (char)0;
            char ac = i < a.Length ? a[i] : (char)127; // Use late ASCII as high surrogate

            if (bc == ac)
            {
                result.Append(bc);
                continue;
            }

            int bIndex = Alphabet.IndexOf(bc);
            int aIndex = Alphabet.IndexOf(ac);

            // Handle characters not in alphabet (padding or boundary)
            if (bIndex == -1) bIndex = -1; // Effectively below 0 ('0')
            if (aIndex == -1) aIndex = Alphabet.Length; // Effectively above others

            int midIndex = (bIndex + aIndex) / 2;

            // If we found a midpoint between the two characters
            if (midIndex > bIndex)
            {
                result.Append(Alphabet[midIndex]);
                break;
            }
            
            // No room at this character position (e.g., '1' and '2') — go one level deeper
            result.Append(bc);
            
            // If b has no more characters, we start from the bottom of the alphabet for the next level
            if (i >= b.Length - 1)
            {
                // We reached the end of 'before', calculate midpoint between Alphabet[0] and Alphabet[max]
                result.Append(Alphabet[Alphabet.Length / 2]);
                break;
            }
        }

        return result.ToString();
    }

    public static string Start() => "V";
    public static string After(string key) => Between(key, null);
    public static string Before(string key) => Between(null, key);
}
