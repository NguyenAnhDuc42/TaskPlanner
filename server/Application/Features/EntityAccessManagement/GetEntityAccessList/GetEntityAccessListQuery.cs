using Application.Common.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public record GetEntityAccessListQuery(Guid LayerId, EntityLayerType LayerType, bool IsManagementMode = false) : IQuery<List<EntityAccessMemberDto>>;

public record EntityAccessMemberDto(
    Guid WorkspaceMemberId,
    Guid UserId,
    string UserName,
    string UserEmail,
    Domain.Enums.Role Role, // Base workspace role
    AccessLevel? ExplicitAccess, // NULL if no direct record exists
    AccessLevel EffectiveAccess,  // Final level after bubble-up/privacy
    bool IsInherited,             // True if EffectiveAccess comes from a parent
    DateTimeOffset CreatedAt,
    bool IsCreator,
    bool IsMe
);
