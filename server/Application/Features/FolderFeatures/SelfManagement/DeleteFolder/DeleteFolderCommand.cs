using System;
using MediatR;

namespace Application.Features.FolderFeatures.SelfManagement.DeleteFolder;

public record DeleteFolderCommand(Guid FolderId) : IRequest<Unit>;
