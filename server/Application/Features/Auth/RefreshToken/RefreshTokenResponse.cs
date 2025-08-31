using System;

namespace Application.Features.Auth.RefreshToken;

public record RefreshTokenResponse(DateTimeOffset accessTokenExpiresAt, DateTimeOffset refreshTokenExpiresAt);