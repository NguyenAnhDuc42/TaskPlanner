namespace Application;

public record BatchUpdateSpaceItemsCommand(
    Guid WorkspaceId,
    List<BatchUpdateSpaceItemValue> Updates
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record BatchUpdateSpaceItemValue(
    Guid Id,
    EntityLayerType Type, // ProjectTask or ProjectFolder
    Guid? StatusId,
    string? Priority,
    string? OrderKey,
    string? PreviousItemOrderKey = null,
    string? NextItemOrderKey = null
);


