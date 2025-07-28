using MediatR;
using src.Domain.Enums;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.AddMembers;

public record class AddMembersRequest(Guid workspaceId,List<string> emails,Role role) : IRequest<Result<AddMembersResponse, ErrorResponse>>;
public record AddMembersBody(List<string> Emails, Role Role);
