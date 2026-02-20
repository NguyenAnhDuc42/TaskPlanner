using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccessBulk;

public record UpdateEntityAccessBulkCommand(
    Guid EntityId,
    EntityLayerType LayerType,
    List<MemberAccessUpdateValue> Members
) : ICommand<Unit>;

public record MemberAccessUpdateValue(
    Guid WorkspaceMemberId,
    AccessLevel? AccessLevel,
    bool IsRemove = false
);
