using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.Workspace.SidebarWorkspaces;

public record class SidebarWorkspacesRequest() : IRequest<Result<List<WorkspaceSummary>, ErrorResponse>>;

