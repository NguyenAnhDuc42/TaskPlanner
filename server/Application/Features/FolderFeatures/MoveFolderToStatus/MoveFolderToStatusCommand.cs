using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures;

public record MoveFolderToStatusCommand(
    Guid FolderId,
    Guid? TargetStatusId,
    string? PreviousItemOrderKey,
    string? NextItemOrderKey,
    string? NewOrderKey = null,
    Domain.Enums.Priority? NewPriority = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;
