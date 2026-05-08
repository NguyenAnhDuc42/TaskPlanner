using System;
using System.Collections.Generic;
using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Features.DocumentFeatures;

public record GetDocumentBlocksQuery(Guid DocumentId) : IQueryRequest<List<DocumentBlockDto>>, IAuthorizedWorkspaceRequest;

public record DocumentBlockDto(
    Guid Id,
    BlockType Type,
    string Content,
    string OrderKey
);
