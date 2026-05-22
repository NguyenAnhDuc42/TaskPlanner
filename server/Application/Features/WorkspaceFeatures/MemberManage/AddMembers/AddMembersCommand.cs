namespace Application;

public record AddMembersCommand(
    Guid workspaceId, 
    List<MemberValue> members, 
    bool? enableEmail, 
    string? message
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record MemberValue(string email, Role role);


