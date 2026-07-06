namespace Api;

public record OAuthCallbackCommand(
    string Provider,
    string ExternalId,
    string Email,
    string Name
) : ICommandRequest<LoginResponse>;
