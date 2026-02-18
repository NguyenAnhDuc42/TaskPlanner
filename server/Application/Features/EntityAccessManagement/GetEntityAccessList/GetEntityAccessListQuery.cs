using Application.Common.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public record GetEntityAccessListQuery(Guid LayerId, EntityLayerType LayerType) : IQuery<List<EntityAccessMemberDto>>;

public record EntityAccessMemberDto(
    Guid WorkspaceMemberId,
    Guid UserId,
    string UserName,
    string UserEmail,
    AccessLevel AccessLevel,
    DateTimeOffset CreatedAt,
    bool IsCreator
);
