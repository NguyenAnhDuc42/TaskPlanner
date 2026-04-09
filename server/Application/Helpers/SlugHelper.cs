using System.Text.RegularExpressions;

namespace Application.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Guid.NewGuid().ToString("N")[..8];

        // Convert to lowercase
        string slug = name.ToLowerInvariant();

        // Remove invalid characters
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

        // Replace multiple spaces/hyphens with a single hyphen
        slug = Regex.Replace(slug, @"[\s-]+", "-").Trim('-');

        // If empty after processing, return a random string
        if (string.IsNullOrEmpty(slug))
            return Guid.NewGuid().ToString("N")[..8];

        return slug;
    }
}
