namespace Api;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommandRequest;
