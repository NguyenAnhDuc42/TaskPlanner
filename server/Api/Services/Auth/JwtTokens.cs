namespace Api;

public record JwtTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpirationAccessToken, DateTimeOffset ExpirationRefreshToken);
