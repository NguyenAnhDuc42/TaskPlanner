using Application.Common.Interfaces;
using Application.Features.Auth.GetCurrentUser;

namespace Application.Features.Auth.UpdateProfile;

public record UpdateProfileCommand(string? Name, string? Email) : ICommand<UserDto>;

