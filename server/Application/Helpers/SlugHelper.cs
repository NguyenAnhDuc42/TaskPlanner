using System.Text.RegularExpressions;
using Domain.Common;

namespace Application.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Guid.NewGuid().ToString("N")[..8];

        var str = name.ToLowerInvariant();
        str = Regex.Replace(str, @"\s+", "-");
        str = Regex.Replace(str, @"[^a-z0-9\-]", "");
        str = str.Trim('-');
        
        return str + "-" + Guid.NewGuid().ToString("N")[..4];
    }
}
