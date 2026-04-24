using Application.Common.Interfaces;

namespace Application.Features.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommandRequest;
