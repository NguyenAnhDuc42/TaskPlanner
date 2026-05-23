namespace Application;

public record GetEntityAccessQuery(Guid SpaceId) : IQueryRequest<IReadOnlyList<EntityAccessRecord>>, IAuthorizedWorkspaceRequest;


