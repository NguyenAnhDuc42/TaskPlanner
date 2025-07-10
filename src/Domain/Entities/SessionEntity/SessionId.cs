using System;

namespace src.Domain.Entities.SessionEntity;

public readonly record struct SessionId(string Value)
{
    private const string Prefix = "SS_";
    
    public static SessionId New() => new(Prefix + Ulid.NewUlid());
    
    public override string ToString() => Value;
    public static implicit operator string(SessionId id) => id.Value;
    
    public static SessionId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SessionId cannot be null or empty", nameof(value));
            
        var trimmed = value.Trim();
        if (!TryParse(trimmed))
            throw new ArgumentException("Invalid SessionId format", nameof(value));
            
        return new SessionId(trimmed);
    }

    public static bool TryParse(string? id)
    {
        return !string.IsNullOrEmpty(id) &&
               id.StartsWith(Prefix, StringComparison.Ordinal) &&
               id.Length > Prefix.Length &&
               Ulid.TryParse(id.AsSpan(Prefix.Length), out _);
    }
}
