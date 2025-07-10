using System;
using System.Security.Claims;
using src.Domain.Entities.UserEntity;
using src.Infrastructure.Services;

namespace src.Infrastructure.Abstractions.IServices;

public interface ITokenService
{
    Task<JwtTokens> GenerateTokensAsync(User user, string userAgent, string ipAddress,CancellationToken cancellationToken);
    Task<JwtTokens?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeTokenAsync(string refreshToken,CancellationToken cancellationToken);
    ClaimsPrincipal? ValidateToken(string token);
}

