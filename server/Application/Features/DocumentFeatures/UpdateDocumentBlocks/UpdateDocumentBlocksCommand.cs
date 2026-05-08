using System;
using System.Collections.Generic;
using Application.Common.Interfaces;

namespace Application.Features.DocumentFeatures;

public record UpdateDocumentBlocksCommand(
    Guid DocumentId,
    List<DocumentBlockValue> Blocks
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record DocumentBlockValue(
    Guid? Id,
    string? Content,
    string? OrderKey,
    Domain.Entities.BlockType? BlockType,
    bool IsDeleted = false
);
