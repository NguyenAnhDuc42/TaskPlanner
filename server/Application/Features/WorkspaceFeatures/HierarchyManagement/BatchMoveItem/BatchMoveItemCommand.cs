using System;
using System.Collections.Generic;
using System.Linq;

namespace Application;

public record MoveSpaceValue(
    Guid ItemId,
    string NewOrderKey
);

public record MoveFolderValue(
    Guid ItemId,
    Guid? TargetParentId, // null = stay in current Space, Value = target SpaceId
    string NewOrderKey
);

public record MoveTaskValue(
    Guid ItemId,
    Guid TargetSpaceId,      // required — task owns its space
    Guid? TargetFolderId,    // optional — organizational focus layer
    string NewOrderKey
);

public record BatchMoveItemCommand(
    List<MoveSpaceValue>? Spaces = null,
    List<MoveFolderValue>? Folders = null,
    List<MoveTaskValue>? Tasks = null
) : ICommandRequest, IAuthorizedWorkspaceRequest
{
    public bool HasAnyMoves =>
        (Spaces?.Any() ?? false) ||
        (Folders?.Any() ?? false) ||
        (Tasks?.Any() ?? false);
}
