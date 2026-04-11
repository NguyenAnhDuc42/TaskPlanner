using Application.Common.Interfaces;

namespace Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommandRequest;
