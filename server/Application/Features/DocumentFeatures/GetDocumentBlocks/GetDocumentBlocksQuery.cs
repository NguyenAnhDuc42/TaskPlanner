using System;
using System.Collections.Generic;
namespace Application;

public record GetDocumentBlocksQuery(Guid DocumentId) : IQueryRequest<List<DocumentBlockDto>>, IAuthorizedWorkspaceRequest;

public record DocumentBlockDto(
    Guid Id,
    BlockType Type,
    string Content,
    string OrderKey
);


