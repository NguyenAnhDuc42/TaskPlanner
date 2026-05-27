using System;
using System.Collections.Generic;

namespace Application;

public record BatchMoveItemValue(
    Guid ItemId,
    EntityLayerType ItemType,
    Guid? TargetParentId,
    string NewOrderKey
);

public record BatchMoveItemCommand(
    List<BatchMoveItemValue> Moves
) : ICommandRequest, IAuthorizedWorkspaceRequest;
