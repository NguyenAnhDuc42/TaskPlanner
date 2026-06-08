using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetSpaceDocumentsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceDocumentsQuery, List<SpaceDocumentRecord>>
{
    public async Task<Result<List<SpaceDocumentRecord>>> Handle(GetSpaceDocumentsQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                d.id AS Id, 
                d.name AS Name,
                true AS IsDefault
            FROM project_spaces s
            JOIN documents d ON d.id = s.default_document_id AND d.deleted_at IS NULL
            WHERE s.id = @SpaceId AND s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL

            UNION ALL

            SELECT 
                d.id AS Id, 
                d.name AS Name,
                false AS IsDefault
            FROM entity_asset_links link
            JOIN documents d ON d.id = link.asset_id AND d.deleted_at IS NULL
            WHERE link.project_space_id = @SpaceId 
              AND link.asset_type = 'Document' 
              AND link.project_workspace_id = @WorkspaceId 
              AND link.deleted_at IS NULL;";

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            SpaceId = request.SpaceId,
            WorkspaceId = workspaceContext.WorkspaceId
        };

        var documents = (await connection.QueryAsync<SpaceDocumentRecord>(sql, parameters)).AsList();
        return Result<List<SpaceDocumentRecord>>.Success(documents);
    }
}
