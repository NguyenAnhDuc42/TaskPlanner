using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Dapper;

namespace Application;

public class GetAvailableStatusesHandler(TaskPlanDbContext db, WorkspaceContext context, HybridCache cache)
    : IQueryHandler<GetAvailableStatusesQuery, List<StatusRecord>>
{
    public async Task<Result<List<StatusRecord>>> Handle(GetAvailableStatusesQuery request, CancellationToken cancellationToken)
    {
        var spaceId = request.SpaceId;

        if (!spaceId.HasValue && request.FolderId.HasValue)
        {
            spaceId = await db.ProjectFolders
                .Where(f => f.Id == request.FolderId.Value)
                .Select(f => (Guid?)f.ProjectSpaceId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!spaceId.HasValue)
            return Result<List<StatusRecord>>.Success([]);

        var cacheKey = $"AvailableStatuses-{spaceId.Value}";

        var response = await cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var connection = db.Database.GetDbConnection();
                return (await connection.QueryAsync<StatusRecord>(@"
                    SELECT id AS Id, project_space_id AS SpaceId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
                    FROM statuses
                    WHERE project_space_id = @SpaceId AND deleted_at IS NULL
                    ORDER BY CASE category
                        WHEN 'NotStarted' THEN 0 WHEN 'Active' THEN 1
                        WHEN 'Done' THEN 2 WHEN 'Closed' THEN 3 ELSE 4 END;",
                    new { SpaceId = spaceId.Value })).AsList();
            },
            tags: [$"Statuses-{context.WorkspaceId}"],
            cancellationToken: cancellationToken
        );

        return Result<List<StatusRecord>>.Success(response);
    }
}
