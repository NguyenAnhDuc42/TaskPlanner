using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using src.Domain.Entities.SessionEntity;
using src.Domain.Entities.UserEntity;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;

namespace src.Infrastructure.Services;

public record JwtSettings
{
    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int Expiration { get; init; } = 15;
    public int RefreshExpiration { get; init; } = 30;
}
public record   JwtTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpirationAccessToken, DateTimeOffset ExpirationRefreshToken);

public class TokenService : ITokenService
{
    private readonly PlannerDbContext _context;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _settings;
    public TokenService(IOptions<JwtSettings> settings, PlannerDbContext context, ISessionRepository sessionRepository, IUserRepository userRepository)
    {
        _settings = settings.Value;
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _userRepository = userRepository ?? throw new ArgumentException(nameof(userRepository));
    }
    public async Task<JwtTokens> GenerateTokensAsync(User user, string userAgent, string ipAddress, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expirationAccess = now.AddMinutes(_settings.Expiration);
        var expirationRefresh = now.AddDays(_settings.RefreshExpiration);

        var accessToken = GenerateAccessToken(user, expirationAccess);
        var refreshToken = GenerateRefreshToken();

        var session = Session.Create(user.Id, refreshToken, expirationRefresh, userAgent, ipAddress);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new JwtTokens(accessToken, refreshToken, expirationAccess, expirationRefresh);

    }

    public async Task<JwtTokens?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetSessionByRefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        if (session == null) return null; //exception and logg in future
        if (session.RevokedAt.HasValue || session.ExspireAt < DateTimeOffset.UtcNow) return null; //exception and logg in future
        var user = await _userRepository.GetUserByIdAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) return null;


        var newExpiration = DateTimeOffset.UtcNow.AddMinutes(_settings.Expiration);
        var newAccessToken = GenerateAccessToken(user, newExpiration);
        return new JwtTokens(newAccessToken, session.RefreshToken, newExpiration, session.ExspireAt);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetSessionByRefreshTokenAsync(refreshToken, cancellationToken);
        if (session != null)
        {
            session.Revoke();
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHanlder = new JwtSecurityTokenHandler();
            var validationParameters = GetValidationParameters();

            var princibal = tokenHanlder.ValidateToken(token, validationParameters, out var _);
            return princibal;
        }
        catch (Exception)
        {
            // exception and logg
            return null;
        }
    }
    private string GenerateAccessToken(User user, DateTimeOffset time)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: time.UtcDateTime,
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
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    }

}


