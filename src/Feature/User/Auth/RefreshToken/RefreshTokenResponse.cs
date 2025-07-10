namespace src.Feature.User.Auth.RefreshToken;

public record RefreshTokenResponse(DateTimeOffset accessTokenExpiresAt, DateTimeOffset refreshTokenExpiresAt, string message = "Refreshed successfully.");
