using System.Security.Claims;
using Application.Features.Auth.DTOs;

using Domain.Entities;

namespace server.Application.Interfaces
{
    public interface ITokenService
    {
        JwtTokens GenerateTokens(User user, string userAgent, string ipAddress);
        JwtTokens RefreshAccessToken(Session session,User user);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
