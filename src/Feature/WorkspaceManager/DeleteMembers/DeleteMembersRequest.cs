using MediatR;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.DeleteMembers;

public record class DeleteMembersRequest(List<Guid> memberIds, Guid workspaceId) : IRequest<Result<string, ErrorResponse>>;

