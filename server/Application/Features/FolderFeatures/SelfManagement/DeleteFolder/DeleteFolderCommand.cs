using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures;

public record DeleteFolderCommand(Guid FolderId) : ICommandRequest, IAuthorizedWorkspaceRequest;
