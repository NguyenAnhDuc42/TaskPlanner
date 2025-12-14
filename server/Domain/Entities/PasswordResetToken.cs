using Domain.Common;

namespace Domain.Entities;

public class PasswordResetToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private PasswordResetToken() { } // EF Core

    private PasswordResetToken(Guid userId, string token, DateTimeOffset expiresAt)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsUsed = false;
    }

    public static PasswordResetToken Create(Guid userId, string token, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty.", nameof(token));
        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("Duration must be positive.", nameof(duration));

        var expiresAt = DateTimeOffset.UtcNow.Add(duration);
        return new PasswordResetToken(userId, token, expiresAt);
    }

    public bool IsValid => !IsUsed && ExpiresAt > DateTimeOffset.UtcNow;

    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Token already used.");
        if (ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Token expired.");

        IsUsed = true;
        UsedAt = DateTimeOffset.UtcNow;
    }
}
