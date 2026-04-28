using System.Text.RegularExpressions;

namespace Application.Helpers;

public static partial class SlugHelper
{
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[^a-z0-9\-]", RegexOptions.Compiled)]
    private static partial Regex SpecialCharsRegex();

    public static string GenerateSlug(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Guid.NewGuid().ToString("N")[..8];

        var str = name.ToLowerInvariant();
        
        // Use source-generated regex for maximum performance
        str = WhitespaceRegex().Replace(str, "-");
        str = SpecialCharsRegex().Replace(str, "");
        str = str.Trim('-');
        
        return str + "-" + Guid.NewGuid().ToString("N")[..4];
    }
}
