namespace Application;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommandRequest;


