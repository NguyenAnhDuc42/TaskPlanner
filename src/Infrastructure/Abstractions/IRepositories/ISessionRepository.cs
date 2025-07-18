using System;
using src.Domain.Entities.SessionEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface ISessionRepository
{
    Task<Session?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellation);
    void RemoveSessionByRefreshTokenAsync(string refreshToken);
}
