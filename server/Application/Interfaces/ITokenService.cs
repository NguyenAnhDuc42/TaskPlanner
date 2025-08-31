using System.Security.Claims;
using Application.Features.Auth.DTOs;

using Domain.Entities;

namespace server.Application.Interfaces
{
    public interface ITokenService
    {
        Task<JwtTokens> GenerateTokensAsync(User user, string userAgent, string ipAddress, CancellationToken cancellationToken);
        Task<JwtTokens?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
