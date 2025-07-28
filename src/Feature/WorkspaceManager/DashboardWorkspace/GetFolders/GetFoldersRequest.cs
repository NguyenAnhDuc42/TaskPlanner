using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.DashboardWorkspace.GetFolders;

public record class GetFoldersRequest(Guid workspaceId) : IRequest<Result<List<FolderSummary>,ErrorResponse>>;

