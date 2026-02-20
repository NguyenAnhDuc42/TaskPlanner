using System;
using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate
) : ICommand<Unit>;

