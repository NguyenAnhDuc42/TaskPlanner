namespace Application;

public record BatchUpdateItemsCommand(
    Guid WorkspaceId,
    List<BatchUpdateItemValue> Updates
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record BatchUpdateItemValue(
    Guid Id,
    EntityLayerType Type, // ProjectTask or ProjectFolder
    Guid? StatusId,
    string? Priority,
    string? OrderKey,
    string? PreviousItemOrderKey = null,
    string? NextItemOrderKey = null
);


