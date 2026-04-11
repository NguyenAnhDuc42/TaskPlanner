using Application.Common.Interfaces;

namespace Application.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommandRequest<string?>;
