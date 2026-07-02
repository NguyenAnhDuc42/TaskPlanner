namespace Api;

// NOT IAuthorizedWorkspaceRequest — same reasoning as CreateWorkspace: the caller isn't a
// member of the target workspace yet, so the PermissionDecorator has nothing to check against.
public record JoinWorkspaceByCodeCommand(string JoinCode) : ICommandRequest<JoinWorkspaceByCodeResult>;

public record JoinWorkspaceByCodeResult(Guid WorkspaceId, string MembershipStatus, bool IsNewMember);
