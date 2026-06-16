namespace Application;

public record CreateSpaceCommand(
    string name,
    string color,
    string icon,
    bool isPrivate,
    List<Guid>? memberIdsToInvite = null
) : ICommandRequest<SpaceRecord>, IAuthorizedWorkspaceRequest;

