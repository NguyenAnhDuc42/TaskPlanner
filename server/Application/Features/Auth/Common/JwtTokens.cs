namespace Application.Features.Auth.Common;

public record JwtTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpirationAccessToken, DateTimeOffset ExpirationRefreshToken);

