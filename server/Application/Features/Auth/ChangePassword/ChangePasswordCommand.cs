using MediatR;

namespace Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
