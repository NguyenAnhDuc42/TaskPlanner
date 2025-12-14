using Domain.Common;
using System;

namespace Domain.Entities;

public class Session : Entity
{
    public string RefreshToken { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    public DateTimeOffset? LastTokenRotationAt { get; private set; }

    public bool IsActive => !RevokedAt.HasValue && ExpiresAt > DateTimeOffset.UtcNow;

    private Session() { } // EF Core

    private Session(Guid userId, string refreshToken, DateTimeOffset expiresAt, string userAgent, string ipAddress)
    {
        UserId = userId;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        LastTokenRotationAt = DateTimeOffset.UtcNow;
    }

    public static Session Create(Guid userId, string refreshToken, DateTimeOffset expiresAt, string userAgent, string ipAddress)
    {
        var session = new Session(userId, refreshToken, expiresAt, userAgent, ipAddress);
        return session;
    }

    public void Revoke(DateTimeOffset? revokedAt = null)
    {
        if (RevokedAt.HasValue) return; // already revoked -> idempotent
        RevokedAt = revokedAt ?? DateTimeOffset.UtcNow;
    }

    public void ExtendExpiration(TimeSpan duration)
    {
        if (RevokedAt.HasValue) throw new InvalidOperationException("Cannot extend an already revoked session.");
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
        if (ExpiresAt <= DateTimeOffset.UtcNow) throw new InvalidOperationException("Cannot extend an already expired session.");

        ExpiresAt = ExpiresAt.Add(duration);
    }

    public void RotateToken(string newRefreshToken)
    {
        if (RevokedAt.HasValue) throw new InvalidOperationException("Cannot rotate token on a revoked session.");
        if (string.IsNullOrWhiteSpace(newRefreshToken)) throw new ArgumentException("Token cannot be empty.", nameof(newRefreshToken));

        RefreshToken = newRefreshToken;
        LastTokenRotationAt = DateTimeOffset.UtcNow;
    }
}