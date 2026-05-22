namespace Application;

public record GetSpaceDetailQuery(Guid SpaceId) : IQueryRequest<SpaceRecord>, IAuthorizedWorkspaceRequest;


