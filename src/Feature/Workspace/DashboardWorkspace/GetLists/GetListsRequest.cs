using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.Workspace.DashboardWorkspace.GetLists;

public record class GetListsRequest(Guid workspaceId) : IRequest<Result<List<ListSumary>, ErrorResponse>>;