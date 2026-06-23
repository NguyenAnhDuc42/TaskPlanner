namespace Application;

public record UpdateSpaceStatusesCommand(
    Guid SpaceId,
    List<StatusUpdateValue> Statuses
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record StatusUpdateValue(
    Guid? Id,
    string Name,
    string Color,
    StatusCategory Category,
    string? PreviousOrderKey,
    string? NextOrderKey,
    RowAction Action
);
