using Application.Common.Interfaces;

namespace Application.Features.Auth.UpdateProfile;

public record UpdateProfileCommand(string? Name, string? Email) : ICommandRequest<UpdateProfileDto>;

public record UpdateProfileDto(
    Guid Id,
    string Name,
    string Email
);
