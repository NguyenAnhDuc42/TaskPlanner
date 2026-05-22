
namespace Application;

public record GetFolderItemsQuery(
    Guid FolderId
) : IQueryRequest<TaskViewData>, IAuthorizedWorkspaceRequest;


