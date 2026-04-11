using Application.Common.Interfaces;
using Application.Features.Auth.Login;

namespace Application.Features.Auth.OAuth;

public record ExternalLoginCommand(string Provider, string Token) : ICommandRequest<LoginResponse>;
