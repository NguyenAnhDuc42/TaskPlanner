namespace Application;

public record DeleteSpaceCommand(
    Guid SpaceId
) : ICommandRequest, IAuthorizedWorkspaceRequest;


