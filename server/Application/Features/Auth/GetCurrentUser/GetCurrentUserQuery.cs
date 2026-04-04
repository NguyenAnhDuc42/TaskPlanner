using MediatR;

namespace Application.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto>;

public record UserDto(
    Guid Id,
    string Name,
    string Email
);
