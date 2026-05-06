using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    Guid? StatusId = null,
    bool? IsInheritingWorkflow = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;
