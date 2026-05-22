namespace Application;

public record class LoginCommand(string email, string password) : ICommandRequest<LoginResponse>;

public record LoginResponse(
    DateTimeOffset accessTokenExpiresAt, 
    DateTimeOffset refreshTokenExpiresAt, 
    string message = "Login successful."
);

