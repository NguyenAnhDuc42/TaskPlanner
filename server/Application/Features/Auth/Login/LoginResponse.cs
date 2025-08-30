namespace Application.Features.Auth.Login;

public record LoginResponse(DateTimeOffset accessTokenExpiresAt, DateTimeOffset refreshTokenExpiresAt, string message = "Login successful.");


