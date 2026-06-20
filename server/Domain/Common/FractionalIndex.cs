namespace Domain;
public static class FractionalIndex
{
    private const string D = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int B = 62;
    private const char MidChar = 'V'; // D[31], midpoint of digit set

    // ─── Public API ────────────────────────────────────────────────────────────

    public static string Start()                         => "a0";
    public static string After(string? key)              => GenerateBetween(key, null);
    public static string Before(string? key)             => GenerateBetween(null, key);
    public static string Between(string? lo, string? hi) => GenerateBetween(lo, hi);

    public static bool IsValid(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        try { Parse(key); return true; }
        catch { return false; }
    }

    /// <summary>Returns After(key) when key is valid, otherwise Start().</summary>
    public static string SafeAfter(string? key) =>
        IsValid(key) ? After(key) : Start();

    public static string GenerateBetween(string? lo, string? hi)
    {
        if (lo == null && hi == null) return "a0";
        if (lo != null) Validate(lo);
        if (hi != null) Validate(hi);

        if (lo == null) return BeforeKey(hi!);
        if (hi == null) return AfterKey(lo);

        if (string.CompareOrdinal(lo, hi) >= 0)
            throw new ArgumentException($"lo must be < hi (lo={lo}, hi={hi})");

        return Midpoint(lo, hi);
    }

    // ─── After (append): increment integer part, drop fraction ────────────────

    private static string AfterKey(string key)
    {
        var (p, intPart, _) = Parse(key);
        var inc = Increment(intPart);
        if (inc.Length == intPart.Length)
            return p + inc;

        // Integer overflowed — advance prefix
        if (p == 'Z') return "a0"; // negative → positive boundary (matches npm package)
        if (p == 'z') throw new InvalidOperationException("FractionalIndex: key space exhausted");
        var next = (char)(p + 1);
        // Skip the gap between 'Z'(90) and 'a'(97): '[', '\', ']', '^', '_', '`'
        if (next > 'Z' && next < 'a') next = 'a';
        return next + new string('0', IntLen(next));
    }

    // ─── Before (prepend): decrement integer part, drop fraction ──────────────

    private static string BeforeKey(string key)
    {
        var (p, intPart, _) = Parse(key);
        var dec = Decrement(intPart);
        if (dec != null)
            return p + dec;

        // Integer underflowed — shrink prefix
        if (p == 'a') return "Z" + new string('z', IntLen('Z')); // positive → negative boundary
        var prev = (char)(p - 1);
        // Skip the gap between 'Z'(90) and 'a'(97)
        if (prev > 'Z' && prev < 'a') prev = 'Z';
        if (prev < 'A') throw new InvalidOperationException("FractionalIndex: key space exhausted");
        return prev + new string('z', IntLen(prev));
    }

    // ─── Midpoint between lo and hi ────────────────────────────────────────────

    private static string Midpoint(string lo, string hi)
    {
        var (loP, loInt, _) = Parse(lo);
        var (hiP, hiInt, _) = Parse(hi);

        if (loP == hiP)
        {
            // Same prefix: try to find a midpoint integer
            var mid = IntegerMidpoint(loInt, hiInt); // length == IntLen(loP)
            if (mid != loInt)
                return loP + mid;

            // Integers are adjacent — no room; append a fraction digit after lo
            // We keep appending until we find a key strictly < hi
            var candidate = lo + MidChar;
            if (string.CompareOrdinal(candidate, hi) < 0)
                return candidate;
            return lo + '0' + MidChar; // go deeper
        }

        // Different prefixes — AfterKey(lo) is safe as long as it's < hi
        var after = AfterKey(lo);
        if (string.CompareOrdinal(after, hi) < 0)
            return after;

        // after >= hi (keys are very close): append fraction to lo
        return lo + MidChar;
    }

    // Find midpoint of two fixed-length base-62 strings of the same length
    private static string IntegerMidpoint(string lo, string hi)
    {
        int n = lo.Length; // == hi.Length == IntLen(prefix)
        var result = new char[n];
        int rem = 0;

        for (int i = 0; i < n; i++)
        {
            int l = D.IndexOf(lo[i]);
            int h = D.IndexOf(hi[i]);
            int cur = rem * B + l + h;
            result[i] = D[cur / 2];
            rem = cur % 2;
        }

        return new string(result);
    }

    // ─── Base-62 increment / decrement ────────────────────────────────────────

    private static string Increment(string digits)
    {
        var arr = digits.ToCharArray();
        for (int i = arr.Length - 1; i >= 0; i--)
        {
            int idx = D.IndexOf(arr[i]);
            if (idx < B - 1) { arr[i] = D[idx + 1]; return new string(arr); }
            arr[i] = '0';
        }
        return "1" + new string('0', digits.Length); // full carry → longer
    }

    private static string? Decrement(string digits)
    {
        var arr = digits.ToCharArray();
        for (int i = arr.Length - 1; i >= 0; i--)
        {
            int idx = D.IndexOf(arr[i]);
            if (idx > 0) { arr[i] = D[idx - 1]; return new string(arr).TrimEnd('0').PadLeft(1, '0'); }
            arr[i] = D[B - 1]; // 'z'
        }
        return null; // full borrow → caller shrinks prefix
    }

    // ─── Parsing / validation ─────────────────────────────────────────────────

    private static (char prefix, string intPart, string fracPart) Parse(string key)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Empty key");
        char p = key[0];
        int n = IntLen(p);
        if (key.Length < 1 + n) throw new ArgumentException($"Key too short for prefix '{p}': {key}");
        return (p, key[1..(1 + n)], key[(1 + n)..]);
    }

    private static void Validate(string key) => Parse(key);

    private static int IntLen(char prefix)
    {
        if (prefix >= 'a' && prefix <= 'z') return prefix - 'a' + 1;
        if (prefix >= 'A' && prefix <= 'Z') return 'Z' - prefix + 1;
        throw new ArgumentException($"Invalid fractional-index prefix: '{prefix}'");
    }
}
