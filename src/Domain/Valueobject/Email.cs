using System.Text.RegularExpressions;

namespace src.Domain.Valueobject;

public readonly record struct Email
{
    public string Value { get; }
    
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Email(string value)
    {
        Value = ValidateAndNormalize(value);
    }

    private static string ValidateAndNormalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be null or empty");

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"Invalid email format: {value}");

        return normalized;
    }
    public override string ToString() => Value;
    public static implicit operator string(Email email) => email.Value;
}