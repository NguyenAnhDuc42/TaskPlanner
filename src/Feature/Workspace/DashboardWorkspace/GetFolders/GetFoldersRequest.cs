using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.DashboardWorkspace.GetFolders;

public record class GetFoldersRequest(Guid workspaceId) : IRequest<Result<FolderItems,ErrorResponse>>;

