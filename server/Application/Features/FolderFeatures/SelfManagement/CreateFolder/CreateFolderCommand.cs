using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures.SelfManagement.CreateFolder;

public record class CreateFolderCommand(
    Guid spaceId,
    string name,
    string? description,
    string color,
    string icon,
    bool isPrivate,
    DateTimeOffset? startDate = null,
    DateTimeOffset? dueDate = null
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;