using Dapper;
using Microsoft.EntityFrameworkCore;
namespace Application;

public class GetSpaceItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceItemsQuery, TaskViewData>
{
    public async Task<Result<TaskViewData>> Handle(GetSpaceItemsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = workspaceContext.workspaceId;

        var activeWorkflow = await WorkflowHelper.GetActiveWorkflow(db, workspaceId, 
            request.SpaceId, 
            null, cancellationToken);

        if (activeWorkflow == null)
            return Result<TaskViewData>.Failure(Error.NotFound("Workflow.NotFound", "Active workflow not found for this space"));

        const string sql = @"
            -- 1. Fetch Statuses
            SELECT id AS StatusId, name, color, category
            FROM statuses
            WHERE workflow_id = @WorkflowId
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0
                WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2
                WHEN 'Closed' THEN 3
                ELSE 4
            END;

            -- 2. Fetch Folders
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, priority, start_date AS StartDate, due_date AS DueDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_folders
            WHERE project_space_id = @SpaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
            ORDER BY order_key;

            -- 3. Fetch Tasks
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, priority, due_date AS DueDate, start_date AS StartDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_space_id = @SpaceId AND project_folder_id IS NULL
            ORDER BY order_key;";

        var parameters = new {
            WorkflowId = activeWorkflow.Id,
            WorkspaceId = workspaceId,
            SpaceId = request.SpaceId
        };

        using var multi = await db.Database.GetDbConnection().QueryMultipleAsync(sql, parameters);
        
        var statuses = (await multi.ReadAsync<TaskItemStatusDto>()).ToList();
        var folders = (await multi.ReadAsync<FolderItemDto>()).ToList();
        var tasks = (await multi.ReadAsync<TaskItemDto>()).ToList();

        return Result<TaskViewData>.Success(new TaskViewData(folders, tasks, statuses));
    }
}


