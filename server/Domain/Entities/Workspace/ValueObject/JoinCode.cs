using Domain.Exceptions;

namespace Domain.Entities;

public record struct JoinCode
{
    private const int DefaultLength = 8;
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string Value { get; init; }

    public JoinCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleException("Join code cannot be empty.");
            
        if (value.Length < 4)
            throw new BusinessRuleException("Join code is too short.");

        Value = value.ToUpperInvariant();
    }

    public static JoinCode Generate(int length = DefaultLength)
    {
        var code = new string(Enumerable.Range(0, length)
            .Select(_ => Chars[Random.Shared.Next(Chars.Length)])
            .ToArray());
        return new JoinCode(code);
    }

    public override string ToString() => Value;
    
    public static implicit operator string(JoinCode code) => code.Value;
}
