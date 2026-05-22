using System;
using System.Collections.Generic;
namespace Application;

public record UpdateDocumentBlocksCommand(
    Guid DocumentId,
    List<DocumentBlockValue> Blocks
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record DocumentBlockValue(
    Guid? Id,
    string? Content,
    string? OrderKey,
    BlockType? BlockType,
    bool IsDeleted = false
);



