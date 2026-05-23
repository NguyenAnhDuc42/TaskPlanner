using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application;

public class GetDocumentBlocksHandler(TaskPlanDbContext db) : IQueryHandler<GetDocumentBlocksQuery, List<DocumentBlockRecord>>
{
    public async Task<Result<List<DocumentBlockRecord>>> Handle(GetDocumentBlocksQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                id AS Id, 
                type AS Type, 
                content AS Content, 
                order_key AS OrderKey
            FROM document_blocks
            WHERE document_id = @DocumentId AND deleted_at IS NULL
            ORDER BY order_key;";

        var blocks = await db.Database.SqlQueryRaw<DocumentBlockRecord>(sql, 
            new Npgsql.NpgsqlParameter("DocumentId", request.DocumentId)).ToListAsync(ct);

        return Result<List<DocumentBlockRecord>>.Success(blocks);
    }
}


