using Application.Common.Interfaces;

namespace Application.Features.Auth;

public record UpdateProfileCommand(string? Name, string? Email) : ICommandRequest;

public record UpdateProfileDto(
    Guid Id,
    string Name,
    string Email
);
