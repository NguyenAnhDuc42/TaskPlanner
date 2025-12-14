using Domain.Enums.RelationShip;

namespace Application.Features.EntityMemberManagement.EntityMemberList;

public record EntityMemberDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    AccessLevel AccessLevel,
    DateTimeOffset CreatedAt
);
