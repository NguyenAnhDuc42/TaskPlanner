using Application.Common.Interfaces;

namespace Application.Features.Auth.RefreshToken;

public record class RefreshTokenCommand() : ICommandRequest<RefreshTokenResponse>;
