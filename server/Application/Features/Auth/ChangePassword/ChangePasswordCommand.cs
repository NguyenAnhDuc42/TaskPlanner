namespace Application;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommandRequest;


