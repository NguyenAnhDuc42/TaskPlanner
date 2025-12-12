using MediatR;

namespace Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
