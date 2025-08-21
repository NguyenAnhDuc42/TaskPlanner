using Domain.Common;
using Domain.Events.SessionEvents;
using System;

namespace Domain.Entities;

public class Session : Aggregate
{
    // Public Properties
    public string RefreshToken { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; } // Fixed typo
    public DateTimeOffset? RevokedAt { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;

    public bool IsActive => !RevokedAt.HasValue && ExpiresAt > DateTimeOffset.UtcNow;

    // Constructors
    private Session() { }

    private Session(Guid userId, string refreshToken, DateTimeOffset expiresAt, string userAgent, string ipAddress)
    {
        UserId = userId;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        UserAgent = userAgent;
        IpAddress = ipAddress;
    }

    // Static Factory Methods
    public static Session Create(Guid userId, string refreshToken, DateTimeOffset expiresAt, string userAgent, string ipAddress)
    {
        var session = new Session(userId, refreshToken, expiresAt, userAgent, ipAddress);
        session.AddDomainEvent(new SessionCreatedEvent(session.Id, session.UserId, session.ExpiresAt));
        return session;
    }

    // Public Methods
    public void Revoke()
    {
        if (RevokedAt.HasValue)
            throw new InvalidOperationException("Session is already revoked.");
        if (ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Cannot revoke an expired session.");

        RevokedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new SessionRevokedEvent(Id, UserId));
    }

    public void ExtendExpiration(TimeSpan duration)
    {
        if (RevokedAt.HasValue)
            throw new InvalidOperationException("Cannot extend an already revoked session.");
        if (ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Cannot extend an already expired session.");
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

        ExpiresAt = ExpiresAt.Add(duration);
        AddDomainEvent(new SessionExpirationExtendedEvent(Id, UserId, ExpiresAt));
    }
}