using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.EntityAccessFeatures;

public record GetEntityAccessQuery(Guid SpaceId) : IQueryRequest<IReadOnlyList<EntityAccessDto>>, IAuthorizedWorkspaceRequest;
public record EntityAccessDto(
    Guid WorkspaceMemberId,
    AccessLevel AccessLevel,
    bool HaveAccess
);
