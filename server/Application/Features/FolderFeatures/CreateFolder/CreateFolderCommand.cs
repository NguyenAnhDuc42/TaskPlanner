using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures;

public record class CreateFolderCommand(
    Guid spaceId,
    string name,
    string color,
    string icon,
    bool isPrivate,
    DateTimeOffset? startDate = null,
    DateTimeOffset? dueDate = null
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;