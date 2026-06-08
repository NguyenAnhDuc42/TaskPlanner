namespace Application;

public record RemoveMembersCommand(
    Guid WorkspaceId, 
    List<Guid> MemberIds
) : ICommandRequest, IAuthorizedWorkspaceRequest;

