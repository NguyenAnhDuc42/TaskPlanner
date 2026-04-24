using Application.Common.Interfaces;

namespace Application.Features.Auth;

public record GetCurrentUserQuery : IQueryRequest<GetCurrentUserDto>;

public record GetCurrentUserDto(
    Guid Id,
    string Name,
    string Email
);
