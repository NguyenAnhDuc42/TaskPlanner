using System;
using System.Collections.Generic;
namespace Application;

public record GetDocumentBlocksQuery(Guid DocumentId) : IQueryRequest<List<DocumentBlockRecord>>, IAuthorizedWorkspaceRequest;


