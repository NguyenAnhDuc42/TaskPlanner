using Microsoft.EntityFrameworkCore;
using Dapper;
namespace Application;

public class GetSpaceItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceItemsQuery, GetSpaceItemsResponse>
{
    public async Task<Result<GetSpaceItemsResponse>> Handle(GetSpaceItemsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = workspaceContext.WorkspaceId;

        var activeWorkflow = await WorkflowHelper.GetActiveWorkflow(db, workspaceId, 
            request.SpaceId, 
            null, cancellationToken);

        if (activeWorkflow == null)
            return Result<GetSpaceItemsResponse>.Failure(Error.NotFound("Workflow.NotFound", "Active workflow not found for this space"));

        var sql = @"
            SELECT id AS Id, id AS StatusId, workflow_id AS WorkflowId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
            FROM statuses
            WHERE workflow_id = @WorkflowId
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0
                WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2
                WHEN 'Closed' THEN 3
                ELSE 4
            END;

            SELECT id AS Id, @WorkspaceId AS WorkspaceId, project_space_id AS SpaceId, name AS Name, created_at AS CreatedAt, status_id AS StatusId, priority AS Priority, start_date AS StartDate, due_date AS DueDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_folders
            WHERE project_space_id = @SpaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
            ORDER BY order_key;

            SELECT id AS Id, @WorkspaceId AS WorkspaceId, project_space_id AS SpaceId, project_folder_id AS FolderId, name AS Name, created_at AS CreatedAt, status_id AS StatusId, priority AS Priority, due_date AS DueDate, start_date AS StartDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_space_id = @SpaceId AND project_folder_id IS NULL
            ORDER BY order_key;";

        var connection = db.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(sql, new { WorkflowId = activeWorkflow.Id, WorkspaceId = workspaceId, SpaceId = request.SpaceId });

        var statuses = (await multi.ReadAsync<StatusRecord>()).AsList();
        var folders = (await multi.ReadAsync<FolderRecord>()).AsList();
        var tasks = (await multi.ReadAsync<TaskRecord>()).AsList();

        return Result<GetSpaceItemsResponse>.Success(new GetSpaceItemsResponse(folders, tasks, statuses));
    }
}


