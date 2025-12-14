using System;
using MediatR;

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    List<Guid>? MemberIdsToAdd  // Add members during update
) : IRequest<Unit>;
