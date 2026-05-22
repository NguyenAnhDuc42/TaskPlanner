namespace Application;

public record RegisterCommand(string username, string email, string password) : ICommandRequest<RegisterResponse>;

public record RegisterResponse(Guid id, string name, string email);


