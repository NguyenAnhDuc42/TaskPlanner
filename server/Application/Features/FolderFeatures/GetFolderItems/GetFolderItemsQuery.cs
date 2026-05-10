using Application.Common.Interfaces;
using Application.Features.ViewFeatures; // For TaskViewData

namespace Application.Features.FolderFeatures;

public record GetFolderItemsQuery(
    Guid FolderId
) : IQueryRequest<TaskViewData>, IAuthorizedWorkspaceRequest;
