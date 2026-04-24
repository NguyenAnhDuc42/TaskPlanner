using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;
