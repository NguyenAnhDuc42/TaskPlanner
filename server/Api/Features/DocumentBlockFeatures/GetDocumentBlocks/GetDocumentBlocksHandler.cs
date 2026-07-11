using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Api;

// No RequireMember() here — viewing a document's blocks is a read, not a write, and Guests
// should be able to at least see task/space content, not just Member+ roles. Scoped by
// project_workspace_id directly on document_blocks (no Document row to check ownership through
// anymore — see Document entity removal).
public class GetDocumentBlocksHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext
) : IQueryHandler<GetDocumentBlocksQuery, List<DocumentBlockRecord>>
{
    public async Task<Result<List<DocumentBlockRecord>>> Handle(GetDocumentBlocksQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                id AS Id, document_id AS DocumentId, type AS Type,
                content AS Content, order_key AS OrderKey
            FROM document_blocks
            WHERE document_id = @DocumentId AND project_workspace_id = @WorkspaceId AND deleted_at IS NULL
            ORDER BY order_key;";

        var connection = db.Database.GetDbConnection();
        var blocks = (await connection.QueryAsync<DocumentBlockRecord>(
            sql, new { request.DocumentId, WorkspaceId = workspaceContext.WorkspaceId })).AsList();

        return Result<List<DocumentBlockRecord>>.Success(blocks);
    }
}
