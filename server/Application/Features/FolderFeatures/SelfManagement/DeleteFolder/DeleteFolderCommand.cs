using Application.Common.Interfaces;

namespace Application.Features.FolderFeatures.SelfManagement.DeleteFolder;

public record DeleteFolderCommand(Guid FolderId) : ICommandRequest;
