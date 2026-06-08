namespace Application;

public record AddMembersCommand(
    Guid WorkspaceId, 
    List<MemberValue> Members, 
    bool? EnableEmail, 
    string? Message
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record MemberValue(string Email, Role Role);


