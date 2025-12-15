namespace Application.Contract.UserContract;

public record UserDto(
    Guid Id,
    string Name,
    string Email
);
