using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.SidebarWorkspaces;

public record class SidebarWorkspacesRequest() : IRequest<Result<SidebarWorkspacesResponse, ErrorResponse>>;

