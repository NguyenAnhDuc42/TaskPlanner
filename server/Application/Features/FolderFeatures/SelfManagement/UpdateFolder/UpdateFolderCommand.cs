using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null
) : ICommandRequest;
