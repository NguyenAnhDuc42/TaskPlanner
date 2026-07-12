using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Api;

public class GetEntityChangesHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetEntityChangesQuery, List<ChangeEntryRecord>>
{
    public async Task<Result<List<ChangeEntryRecord>>> Handle(GetEntityChangesQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                id AS Id, entity_type AS EntityType, action AS Action,
                author_user_id AS AuthorMemberId, created_at AS CreatedAt
            FROM sync_events
            WHERE entity_id = @EntityId
              AND entity_type = @EntityType
              AND project_workspace_id = @WorkspaceId
            ORDER BY created_at DESC, id DESC
            LIMIT 30;";

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            EntityId = request.EntityId,
            EntityType = request.EntityType.ToString(),
            WorkspaceId = workspaceContext.WorkspaceId,
        };

        var changes = (await connection.QueryAsync<ChangeEntryRecord>(sql, parameters)).AsList();

        return Result<List<ChangeEntryRecord>>.Success(changes);
    }
}
