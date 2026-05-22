namespace Application;

public record GetEntityAccessQuery(Guid SpaceId) : IQueryRequest<IReadOnlyList<EntityAccessDto>>, IAuthorizedWorkspaceRequest;
public record EntityAccessDto(
    Guid WorkspaceMemberId,
    AccessLevel AccessLevel,
    bool HaveAccess
);


