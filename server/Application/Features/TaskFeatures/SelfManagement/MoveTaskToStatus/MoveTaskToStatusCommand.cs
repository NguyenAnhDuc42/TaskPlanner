using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures;

public record MoveTaskToStatusCommand(
    Guid TaskId,
    Guid? TargetStatusId,
    string? PreviousItemOrderKey,
    string? NextItemOrderKey,
    string? NewOrderKey = null,
    Domain.Enums.Priority? NewPriority = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;
