using Application.Common.Interfaces;

namespace Application.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery : IQueryRequest<GetCurrentUserDto>;

public record GetCurrentUserDto(
    Guid Id,
    string Name,
    string Email
);
