namespace Application;

public record class TransferOwnershipCommand(Guid WorkspaceId, Guid NewOwnerId) : ICommandRequest, IAuthorizedWorkspaceRequest;


