using System.Text.RegularExpressions;

namespace Domain.Common
{
    /// <summary>
    /// Minimal color validation helper.
    /// Supports: #RRGGBB, #RGB, RRGGBB and RGB (case-insensitive).
    /// Add more rules (named CSS colors, rgba(...), etc.) if you need them.
    /// </summary>
    public static class ColorValidator
    {
        // Accepts: #RRGGBB, #RGB, RRGGBB, RGB
        private static readonly Regex HexColorRegex = new Regex(
            @"^#?(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsValidColorCode(string? color)
        {
            if (string.IsNullOrWhiteSpace(color)) return false;
            return HexColorRegex.IsMatch(color.Trim());
        }
    }
}