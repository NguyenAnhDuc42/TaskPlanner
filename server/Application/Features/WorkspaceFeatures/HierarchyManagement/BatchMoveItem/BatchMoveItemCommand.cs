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

public record BatchMoveItemCommand : ICommandRequest, IAuthorizedWorkspaceRequest
{
    public List<MoveSpaceValue> Spaces { get; init; } = [];
    public List<MoveFolderValue> Folders { get; init; } = [];
    public List<MoveTaskValue> Tasks { get; init; } = [];

    public bool HasAnyMoves => Spaces.Count > 0 || Folders.Count > 0 || Tasks.Count > 0;
}
