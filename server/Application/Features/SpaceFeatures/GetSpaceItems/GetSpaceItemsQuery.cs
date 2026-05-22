
namespace Application;

public record GetSpaceItemsQuery(
    Guid SpaceId
) : IQueryRequest<TaskViewData>, IAuthorizedWorkspaceRequest;


