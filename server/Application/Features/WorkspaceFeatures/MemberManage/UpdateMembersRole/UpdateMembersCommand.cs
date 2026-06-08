namespace Application;

public record class UpdateMembersCommand(Guid WorkspaceId, List<UpdateMemberValue> Members) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record class UpdateMemberValue(Guid MemberId, Role? Role, MembershipStatus? Status);

