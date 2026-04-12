using Application.Common.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccessBulk;

public record UpdateEntityAccessBulkCommand(
    Guid EntityId,
    EntityLayerType LayerType,
    List<MemberAccessUpdateValue> Members
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record MemberAccessUpdateValue(
    Guid WorkspaceMemberId,
    AccessLevel? AccessLevel,
    bool IsRemove = false
);
