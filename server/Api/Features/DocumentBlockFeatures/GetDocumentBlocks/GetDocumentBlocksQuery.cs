namespace Api;

public record GetDocumentBlocksQuery(Guid DocumentId) : IQueryRequest<List<DocumentBlockRecord>>, IAuthorizedWorkspaceRequest;
