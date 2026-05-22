using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.FolderFeatures;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Color,
    string? Icon,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    Guid? StatusId = null,
    Priority? Priority = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;
