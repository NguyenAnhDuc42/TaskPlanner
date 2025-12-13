using System;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using server.Application.Interfaces;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Application.Features.Auth.DTOs;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Auth;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IOptions<JwtSettings> jwtSettings, ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<JwtTokens> GenerateTokensAsync(User user, string userAgent, string ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expirationAccess = now.AddMinutes(_jwtSettings.Expiration);
        var expirationRefresh = now.AddDays(_jwtSettings.RefreshExpiration);

        var accessToken = GenerateAccessToken(user, expirationAccess);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        return new JwtTokens(accessToken, refreshToken, expirationAccess, expirationRefresh);
    }

    public async Task<JwtTokens> RefreshAccessTokenAsync(Session session,User user)
    { 

        var newExpiration = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.Expiration);
        var newAccessToken = GenerateAccessToken(user, newExpiration);
            
        return new JwtTokens(newAccessToken, session.RefreshToken, newExpiration, session.ExpiresAt);
    }


    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed.");
            throw;
        }
    }

   

    private string GenerateAccessToken(User user, DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime, // JwtSecurityToken only accepts DateTime
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }

    private TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    }
}
