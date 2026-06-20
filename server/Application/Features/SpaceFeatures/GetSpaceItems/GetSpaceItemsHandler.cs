using Dapper;
using Microsoft.EntityFrameworkCore;
namespace Application;

public class GetSpaceItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceItemsQuery, GetSpaceItemsResponse>
{
    public async Task<Result<GetSpaceItemsResponse>> Handle(GetSpaceItemsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = workspaceContext.WorkspaceId;
        var isOwner = workspaceContext.CurrentMember.Role == Domain.Role.Owner;
        var connection = db.Database.GetDbConnection();

        var checkAccessSql = @"
            SELECT 1 FROM project_spaces s
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id 
                AND ea.workspace_member_id = @MemberId 
                AND ea.deleted_at IS NULL
            WHERE s.id = @SpaceId AND s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
              AND (s.is_private = false OR ea.id IS NOT NULL OR @IsOwner = true);";

        var hasAccess = await connection.ExecuteScalarAsync<bool>(checkAccessSql, new {
            SpaceId = request.SpaceId,
            WorkspaceId = workspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        });

        if (!hasAccess)
            return Result<GetSpaceItemsResponse>.Failure(Error.NotFound("Space.NotFound", "Space not found or access denied"));

        var activeWorkflow = await WorkflowHelper.GetActiveWorkflow(db, workspaceId, request.SpaceId, null, cancellationToken);
        if (activeWorkflow == null)
            return Result<GetSpaceItemsResponse>.Failure(Error.NotFound("Workflow.NotFound", "Active workflow not found for this space"));

        const string sql = @"
            SELECT id AS Id, id AS StatusId, workflow_id AS WorkflowId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
            FROM statuses
            WHERE workflow_id = @WorkflowId
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0 WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2 WHEN 'Closed' THEN 3 ELSE 4 END;

            SELECT f.id AS Id, @WorkspaceId AS WorkspaceId, f.project_space_id AS SpaceId,
                   f.name AS Name, f.created_at AS CreatedAt, f.status_id AS StatusId,
                   f.priority AS Priority, f.start_date AS StartDate, f.due_date AS DueDate,
                   f.order_key AS OrderKey, f.custom_icon AS Icon, f.custom_color AS Color,
                   CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_folders f
            LEFT JOIN favorites fav ON fav.entity_id = f.id AND fav.workspace_member_id = @MemberId
            WHERE f.project_space_id = @SpaceId AND f.deleted_at IS NULL AND f.is_archived = false
            ORDER BY f.order_key;

            SELECT t.id AS Id, @WorkspaceId AS WorkspaceId, t.project_space_id AS SpaceId,
                   t.project_folder_id AS FolderId, t.name AS Name, t.created_at AS CreatedAt,
                   t.status_id AS StatusId, t.priority AS Priority,
                   t.due_date AS DueDate, t.start_date AS StartDate,
                   t.order_key AS OrderKey, t.custom_icon AS Icon, t.custom_color AS Color,
                   t.parent_task_id AS ParentTaskId,
                   CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_tasks t
            LEFT JOIN favorites fav ON fav.entity_id = t.id AND fav.workspace_member_id = @MemberId
            WHERE t.project_workspace_id = @WorkspaceId
              AND t.project_space_id = @SpaceId
              AND t.deleted_at IS NULL AND t.is_archived = false
              AND t.project_folder_id IS NULL AND t.parent_task_id IS NULL
            ORDER BY t.order_key;";

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            WorkflowId  = activeWorkflow.Id,
            WorkspaceId = workspaceId,
            SpaceId     = request.SpaceId,
            MemberId    = workspaceContext.CurrentMember.Id
        });

        var statuses = (await multi.ReadAsync<StatusRecord>()).AsList();
        var folders  = (await multi.ReadAsync<FolderRecord>()).AsList();
        var tasks    = (await multi.ReadAsync<TaskRecord>()).AsList();

        return Result<GetSpaceItemsResponse>.Success(new GetSpaceItemsResponse(folders, tasks, statuses));
    }
}
