using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Dapper;

namespace Application;

public class GetWorkspaceStatusesHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, HybridCache cache) : IQueryHandler<GetWorkspaceStatusesQuery, List<StatusRecord>>
{
    public async Task<Result<List<StatusRecord>>> Handle(GetWorkspaceStatusesQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = workspaceContext.WorkspaceId;
        var cacheKey = $"Statuses-{workspaceId}";

        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                const string sql = @"
                    SELECT s.id AS Id, s.project_space_id AS SpaceId, s.name AS Name,
                           s.color AS Color, s.category AS Category, s.order_key AS OrderKey
                    FROM statuses s
                    INNER JOIN project_spaces sp ON sp.id = s.project_space_id AND sp.deleted_at IS NULL
                    WHERE s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
                    ORDER BY s.project_space_id, s.category, s.order_key;";

                var connection = db.Database.GetDbConnection();
                return (await connection.QueryAsync<StatusRecord>(sql, new { WorkspaceId = workspaceId })).AsList();
            },
            tags: [$"Statuses-{workspaceId}"],
            cancellationToken: cancellationToken
        );

        return Result<List<StatusRecord>>.Success(result);
    }
}
