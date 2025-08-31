namespace Application.Features.Auth.DTOs;

public record JwtTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpirationAccessToken, DateTimeOffset ExpirationRefreshToken);

