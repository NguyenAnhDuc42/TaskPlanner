using Application.Common.Interfaces;

namespace Application.Features.Auth;

public record ForgotPasswordCommand(string Email) : ICommandRequest<string?>;
