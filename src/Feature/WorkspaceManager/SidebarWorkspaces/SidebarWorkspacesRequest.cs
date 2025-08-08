using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.SidebarWorkspaces;

public record class SidebarWorkspacesRequest(Guid workspaceId) : IRequest<Result<GroupWorkspace, ErrorResponse>>;

