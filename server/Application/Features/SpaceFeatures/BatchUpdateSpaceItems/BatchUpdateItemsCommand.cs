namespace Application;

public record BatchUpdateSpaceItemsCommand(
    Guid WorkspaceId,
    Guid SpaceId,
    List<BatchUpdateSpaceItemValue> Updates
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record BatchUpdateSpaceItemValue(
    Guid Id,
    EntityLayerType Type, 
    Guid? StatusId,
    Priority? Priority,
    string? OrderKey,
    string? PreviousItemOrderKey = null,
    string? NextItemOrderKey = null
);


