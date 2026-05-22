namespace Application;

public record UpdateProfileCommand(string? Name, string? Email) : ICommandRequest;

public record UpdateProfileDto(
    Guid Id,
    string Name,
    string Email
);


