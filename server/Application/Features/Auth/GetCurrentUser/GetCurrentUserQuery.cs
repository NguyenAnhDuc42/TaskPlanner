namespace Application;

public record GetCurrentUserQuery : IQueryRequest<GetCurrentUserDto>;

public record GetCurrentUserDto(
    Guid Id,
    string Name,
    string Email
);


