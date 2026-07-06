namespace Api;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommandRequest;
