using Application.Common.Interfaces;

namespace Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommandRequest;
