using src.Infrastructure.Services;

namespace src.Feature.User.Auth;

public record LoginResponse(DateTimeOffset accessTokenExpiresAt, DateTimeOffset refreshTokenExpiresAt, string message = "Login successful.");

