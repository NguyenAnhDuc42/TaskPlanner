using System;
using MediatR;

namespace Application.Features.Auth.RefreshToken;

public record class RefreshTokenCommand() : IRequest<RefreshTokenResponse>;
