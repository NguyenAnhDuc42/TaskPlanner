using System;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.FolderFeatures.SelfManagement.DeleteFolder;

public record DeleteFolderCommand(Guid FolderId) : ICommand<Unit>;
