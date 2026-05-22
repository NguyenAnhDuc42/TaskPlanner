namespace Application;

public record GetAvailableStatusesQuery(
    Guid? SpaceId = null,
    Guid? FolderId = null
) : IQueryRequest<List<StatusRecord>>;


