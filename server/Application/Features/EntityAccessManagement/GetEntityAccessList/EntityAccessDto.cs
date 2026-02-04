using Domain.Enums.RelationShip;

namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public record EntityAccessDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    AccessLevel AccessLevel,
    DateTimeOffset CreatedAt
);
