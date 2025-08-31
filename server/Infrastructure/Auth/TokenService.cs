using System;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using server.Application.Interfaces;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Application.Features.Auth.DTOs;

namespace Infrastructure.Auth;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<TokenService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public TokenService(IOptions<JwtSettings> jwtSettings, IUnitOfWork unitOfWork, ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger;
    }

    public async Task<JwtTokens> GenerateTokensAsync(User user, string userAgent, string ipAddress, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expirationAccess = now.AddMinutes(_jwtSettings.Expiration);
        var expirationRefresh = now.AddDays(_jwtSettings.RefreshExpiration);

        var accessToken = GenerateAccessToken(user, expirationAccess);
        var refreshToken = GenerateRefreshToken();

        var session = Session.Create(user.Id, refreshToken, expirationRefresh, userAgent, ipAddress);
        await _unitOfWork.Sessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new JwtTokens(accessToken, refreshToken, expirationAccess, expirationRefresh);
    }

    public async Task<JwtTokens?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByRefreshToken(refreshToken, cancellationToken);
        if (session == null) { _logger.LogWarning("Refresh token not found."); throw new SecurityTokenException("Invalid refresh token."); }
        if (session.RevokedAt.HasValue) { _logger.LogWarning("Refresh token revoked."); throw new SecurityTokenException("Invalid refresh token."); }
        if (session.ExpiresAt <= DateTimeOffset.UtcNow) { _logger.LogWarning("Refresh token expired."); throw new SecurityTokenExpiredException("Refresh token expired."); }

        var user = await _unitOfWork.Users.GetByIdAsync(session.UserId, cancellationToken);
        if (user == null) { _logger.LogWarning("User not found for the given refresh token."); throw new SecurityTokenException("Invalid refresh token."); }

        var newExpiration = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.Expiration);
        var newAccessToken = GenerateAccessToken(user, newExpiration);

        return new JwtTokens(newAccessToken, refreshToken, newExpiration, session.ExpiresAt);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByRefreshToken(refreshToken, cancellationToken);
        if (session == null) { _logger.LogWarning("Refresh token not found for revocation."); throw new SecurityTokenException("Invalid refresh token."); }

        session.Revoke();
        _unitOfWork.Sessions.Update(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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

    public static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
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
