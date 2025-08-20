using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.GetHierarchy;

public record class GetHierarchyRequest(Guid workspaceId) : IRequest<Result<Hierarchy,ErrorResponse>>;

