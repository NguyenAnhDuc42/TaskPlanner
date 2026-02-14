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
    bool? IsPrivate,
    List<UpdateFolderMemberValue>? MembersToAddOrUpdate
) : ICommand<Unit>;

public record UpdateFolderMemberValue(Guid workspaceMemberId, AccessLevel? accessLevel, bool isRemove = false);
