namespace Application;

public record ReorderStatusesCommand(
    Guid StatusId,
    string? PreviousStatusOrderKey,
    string? NextStatusOrderKey,
    string? NewOrderKey = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;


