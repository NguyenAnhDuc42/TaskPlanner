using System.Security.Claims;
using Application.Features.Auth.DTOs;

using Domain.Entities;

namespace server.Application.Interfaces
{
    public interface ITokenService
    {
        Task<JwtTokens> GenerateTokensAsync(User user, string userAgent, string ipAddress, CancellationToken cancellationToken);
        Task<JwtTokens> RefreshAccessTokenAsync(Session session,User user);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
