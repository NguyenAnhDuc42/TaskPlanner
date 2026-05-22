namespace Application;

public record ExternalLoginCommand(string Provider, string Token) : ICommandRequest<LoginResponse>;


