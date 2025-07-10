
public readonly record struct UserId(string Value)
{
    private const string Prefix = "U_";

    public static UserId New() => new(Prefix + Ulid.NewUlid());

    public override string ToString() => Value;
    public static implicit operator string(UserId id) => id.Value;

    public static UserId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("UserId cannot be null or empty", nameof(value));

        var trimmed = value.Trim();
        if (!TryParse(trimmed))
            throw new ArgumentException("Invalid UserId format", nameof(value));

        return new UserId(trimmed);
    }

    public static bool TryParse(string? id)
    {
        return !string.IsNullOrEmpty(id) &&
               id.StartsWith(Prefix, StringComparison.Ordinal) &&
               id.Length > Prefix.Length &&
               Ulid.TryParse(id.AsSpan(Prefix.Length), out _);
    }
}