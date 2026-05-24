namespace Application;

public record MoveItemCommand(
    Guid ItemId,
    EntityLayerType ItemType,
    Guid? TargetParentId,           // New parent (Space for Folder, Folder/Space for Task)
    Guid? SourceParentId,           // Old parent to notify frontend for invalidation
    string? PreviousItemOrderKey,   // OrderKey of item above
    string? NextItemOrderKey,        // OrderKey of item below
    string? NewOrderKey             // Optional: Pre-calculated key from frontend
) : ICommandRequest, IAuthorizedWorkspaceRequest;


