namespace Api;

public record ForgotPasswordCommand(string Email) : ICommandRequest<string?>;
