using System;

namespace Application.Common.Utils;

public static class EnumParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        // Use the built-in Enum.TryParse with case-insensitivity.
        return Enum.TryParse(value, ignoreCase: true, out result);
    }
    public static TEnum ParseOrDefault<TEnum>(string? value, TEnum defaultValue) where TEnum : struct, Enum
    {
        return TryParse(value, out TEnum result) ? result : defaultValue;
    }
    public static TEnum ParseOrThrow<TEnum>(string? value, string? parameterName = null) where TEnum : struct, Enum
    {
        if (TryParse(value, out TEnum result))
        {
            return result;
        }

        // If parsing fails, throw a descriptive exception.
        var friendlyName = parameterName ?? "value";
        var enumName = typeof(TEnum).Name;
        throw new ArgumentException($"The value '{value}' is not a valid {enumName}.", friendlyName);
    }
}
