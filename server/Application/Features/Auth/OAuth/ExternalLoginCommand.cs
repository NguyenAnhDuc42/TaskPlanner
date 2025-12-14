using Application.Features.Auth.Login;
using MediatR;

namespace Application.Features.Auth.OAuth;

public record ExternalLoginCommand(string Provider, string Token) : IRequest<LoginResponse>;
