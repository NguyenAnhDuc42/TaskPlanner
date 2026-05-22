namespace Application;

public record RemoveMembersCommand(
    Guid workspaceId, 
    List<Guid> memberIds
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;

