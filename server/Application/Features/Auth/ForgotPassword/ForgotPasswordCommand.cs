namespace Application;

public record ForgotPasswordCommand(string Email) : ICommandRequest<string?>;


