using Domain.Common;
using System;

namespace Domain.Entities;

public class Session : Entity
{
    // Public Properties
    public string RefreshToken { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExspireAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;

    // Constructors
    private Session() { }

    private Session(Guid userId, string refreshToken, DateTimeOffset exspireAt, string userAgent, string ipAddress)
    {
        UserId = userId;
        RefreshToken = refreshToken;
        ExspireAt = exspireAt;
        UserAgent = userAgent;
        IpAddress = ipAddress;
    }

    // Static Factory Methods
    public static Session Create(Guid userId, string refreshToken, DateTimeOffset exspireAt, string userAgent, string ipAddress)
    {
        return new Session(userId, refreshToken, exspireAt, userAgent, ipAddress);
    }

    // Public Methods
    public void Revoke()
    {
        RevokedAt = DateTimeOffset.UtcNow;
    }
}