using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.DashboardWorkspace.GetLists;

public record class GetListsRequest(Guid workspaceId) : IRequest<Result<ListItems, ErrorResponse>>;