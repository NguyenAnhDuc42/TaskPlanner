using MediatR;
using src.Domain.Enums;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.UpdateMembers;

public record class UpdateMembersRequest(Guid WorkspaceId,List<Guid> MemberIds,Role Role) : IRequest<Result<string,ErrorResponse>>;
public record class UpdateMembersBody(List<Guid> MemberIds, Role Role);
