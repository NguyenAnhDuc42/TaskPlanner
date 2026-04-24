namespace Application.Features.Auth;

public record JwtTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpirationAccessToken, DateTimeOffset ExpirationRefreshToken);

