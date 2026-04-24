using Application.Common.Interfaces;

namespace Application.Features.Auth;

public record ExternalLoginCommand(string Provider, string Token) : ICommandRequest<LoginResponse>;
