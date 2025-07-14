using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.GetHierarchy;

public record class GetHierarchyRequest(Guid workspaceId) : IRequest<Result<Hierarchy,ErrorResponse>>;

