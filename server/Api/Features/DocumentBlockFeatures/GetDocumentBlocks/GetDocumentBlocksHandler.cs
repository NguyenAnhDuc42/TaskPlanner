using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Api;

public class GetDocumentBlocksHandler(
    TaskPlanDbContext db,
    SyncPermissionService syncPermission
) : IQueryHandler<GetDocumentBlocksQuery, List<DocumentBlockRecord>>
{
    public async Task<Result<List<DocumentBlockRecord>>> Handle(GetDocumentBlocksQuery request, CancellationToken cancellationToken)
    {
        var document = await db.Documents.AsNoTracking()
            .Where(d => d.Id == request.DocumentId && d.DeletedAt == null)
            .Select(d => new { d.ProjectWorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null) return Result<List<DocumentBlockRecord>>.Failure(DocumentError.NotFound);

        syncPermission.RequireMember();

        const string sql = @"
            SELECT
                id AS Id, document_id AS DocumentId, type AS Type,
                content AS Content, order_key AS OrderKey
            FROM document_blocks
            WHERE document_id = @DocumentId AND deleted_at IS NULL
            ORDER BY order_key;";

        var connection = db.Database.GetDbConnection();
        var blocks = (await connection.QueryAsync<DocumentBlockRecord>(
            sql, new { request.DocumentId })).AsList();

        return Result<List<DocumentBlockRecord>>.Success(blocks);
    }
}
