using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Caching.Hybrid;
using Dapper;

namespace Application;

public class GetWorkspaceWorkflowsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, HybridCache cache) : IQueryHandler<GetWorkspaceWorkflowsQuery, List<WorkflowRecord>>
{
    public async Task<Result<List<WorkflowRecord>>> Handle(GetWorkspaceWorkflowsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = workspaceContext.WorkspaceId;
        var cacheKey = $"Workflows-{workspaceId}-{request.LayerType}-{request.LayerId}";
        
        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async cancelToken =>
            {
                const string sql = @"
            SELECT 
                w.id AS Id, w.name AS Name, w.project_space_id AS ProjectSpaceId, w.project_folder_id AS ProjectFolderId,
                s.id AS StatusId, s.name AS StatusName, s.color AS Color, s.category AS Category, s.order_key AS OrderKey
            FROM workflows w
            LEFT JOIN statuses s ON w.id = s.workflow_id
            WHERE w.project_workspace_id = @WorkspaceId 
              AND (@LayerId IS NULL OR 
                  (@LayerType = 'space' AND w.project_space_id = @LayerId) OR 
                  (@LayerType = 'folder' AND w.project_folder_id = @LayerId))
              AND w.deleted_at IS NULL AND (s.deleted_at IS NULL OR s.id IS NULL)
            ORDER BY w.name, s.category, s.order_key;";

                var connection = db.Database.GetDbConnection();
                var parameters = new
                {
                    WorkspaceId = workspaceId,
                    LayerId = request.LayerId,
                    LayerType = request.LayerType
                };

                var rows = (await connection.QueryAsync<WorkflowRow>(sql, parameters)).AsList();

                return rows
                    .GroupBy(r => new { r.Id, r.Name, r.ProjectSpaceId, r.ProjectFolderId })
                    .Select(g => new WorkflowRecord
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        ProjectSpaceId = g.Key.ProjectSpaceId,
                        ProjectFolderId = g.Key.ProjectFolderId,
                        Statuses = g.Where(r => r.StatusId != null).Select(r => new StatusRecord
                        {
                            Id = r.StatusId!.Value,
                            WorkflowId = g.Key.Id,
                            Name = r.StatusName!,
                            Color = r.Color,
                            Category = r.Category!.Value,
                            OrderKey = r.OrderKey
                        }).ToList()
                    }).ToList();
            },
            tags: [$"Workflows-{workspaceContext.WorkspaceId}"],
            cancellationToken: cancellationToken
        );

        return Result<List<WorkflowRecord>>.Success(result);
    }

    private record WorkflowRow(
        Guid Id, string Name, Guid? ProjectSpaceId, Guid? ProjectFolderId,
        Guid? StatusId, string? StatusName, string? Color, StatusCategory? Category, string? OrderKey
    );
}


