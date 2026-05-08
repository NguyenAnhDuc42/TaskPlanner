using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.DocumentFeatures;

public class GetDocumentBlocksHandler(IDataBase db) : IQueryHandler<GetDocumentBlocksQuery, List<DocumentBlockDto>>
{
    public async Task<Result<List<DocumentBlockDto>>> Handle(GetDocumentBlocksQuery request, CancellationToken ct)
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

        var blocks = await db.Connection.QueryAsync<DocumentBlockDto>(sql, new { request.DocumentId });

        return Result<List<DocumentBlockDto>>.Success(blocks.ToList());
    }
}
