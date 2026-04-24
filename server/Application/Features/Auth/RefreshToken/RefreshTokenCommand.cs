using Application.Common.Interfaces;

namespace Application.Features.Auth;

public record class RefreshTokenCommand() : ICommandRequest<RefreshTokenResponse>;

public record RefreshTokenResponse(DateTimeOffset accessTokenExpiresAt, DateTimeOffset refreshTokenExpiresAt);
