using System;

namespace src.Domain.Entities.SessionEntity;

public class Session : Entity<Guid>
{
    public string RefreshToken { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public DateTimeOffset ExspireAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string UserAgent { get; private set; } = string.Empty;
    public string IpAddress { get; private set; } = string.Empty;
    private Session() { }

    private Session(Guid sessionId, Guid userId, string refreshToken, DateTimeOffset exspireAt, string userAgent, string ipAddress) : base(sessionId)
    {
        UserId = userId;
        RefreshToken = refreshToken;
        ExspireAt = exspireAt;
        UserAgent = userAgent;
        IpAddress = ipAddress;
    }

    public static Session Create(Guid userId, string refreshToken, DateTimeOffset exspireAt, string userAgent, string ipAddress)
    {
        return new Session(Guid.NewGuid(), userId, refreshToken, exspireAt, userAgent, ipAddress);
    }
    public void Revoke()
    {
        RevokedAt = DateTimeOffset.UtcNow;
    }


}
