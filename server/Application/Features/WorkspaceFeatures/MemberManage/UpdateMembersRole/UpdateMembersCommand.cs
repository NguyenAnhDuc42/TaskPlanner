namespace Application;

public record class UpdateMembersCommand(Guid workspaceId, List<UpdateMemberValue> members) : ICommandRequest<Guid>;

public record class UpdateMemberValue(Guid userId, Role? role, MembershipStatus? status);

