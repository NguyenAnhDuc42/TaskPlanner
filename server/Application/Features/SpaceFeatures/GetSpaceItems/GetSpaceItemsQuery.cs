
namespace Application;

public record GetSpaceItemsQuery(
    Guid SpaceId
) : IQueryRequest<GetSpaceItemsResponse>, IAuthorizedWorkspaceRequest;

public record GetSpaceItemsResponse(
    List<FolderRecord> Folders,
    List<TaskRecord> Tasks,
    List<StatusRecord> Statuses
);



