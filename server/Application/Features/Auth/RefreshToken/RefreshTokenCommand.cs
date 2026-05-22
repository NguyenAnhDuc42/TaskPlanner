namespace Application;

public record class RefreshTokenCommand() : ICommandRequest<RefreshTokenResponse>;

public record RefreshTokenResponse(DateTimeOffset accessTokenExpiresAt, DateTimeOffset refreshTokenExpiresAt);


