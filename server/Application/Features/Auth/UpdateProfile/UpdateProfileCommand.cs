using Application.Common.Interfaces;
using Application.Contract.UserContract;

namespace Application.Features.Auth.UpdateProfile;

public record UpdateProfileCommand(string? Name, string? Email) : ICommand<UserDto>;

