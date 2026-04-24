using Application.Common.Interfaces;

namespace Application.Features.Auth;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommandRequest;
