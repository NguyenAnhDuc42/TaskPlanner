using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetFolderItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderItemsQuery, TaskViewData>
{
    public async Task<Result<TaskViewData>> Handle(GetFolderItemsQuery request, CancellationToken ct)
    {
        var workspaceId = workspaceContext.workspaceId;
        var connection = db.Database.GetDbConnection();

        // Get SpaceId for the folder to resolve workflow
        var spaceId = await connection.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT project_space_id FROM project_folders WHERE id = @FolderId AND deleted_at IS NULL",
            new { FolderId = request.FolderId });

        if (spaceId == null)
            return Result<TaskViewData>.Failure(Error.NotFound("Folder.NotFound", "Folder not found"));

        var activeWorkflow = await WorkflowHelper.GetActiveWorkflow(db, workspaceId, 
            spaceId.Value, 
            request.FolderId, ct);

        if (activeWorkflow == null)
            return Result<TaskViewData>.Failure(Error.NotFound("Workflow.NotFound", "Active workflow not found for this folder"));

        var sql = @"
            SELECT id AS Id, id AS StatusId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
            FROM statuses
            WHERE workflow_id = @WorkflowId
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0
                WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2
                WHEN 'Closed' THEN 3
                ELSE 4
            END;

            SELECT id AS Id, name AS Name, created_at AS CreatedAt, status_id AS StatusId, priority AS Priority, due_date AS DueDate, start_date AS StartDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_folder_id = @FolderId
            ORDER BY order_key;";

        using var multi = await connection.QueryMultipleAsync(sql, new { WorkflowId = activeWorkflow.Id, WorkspaceId = workspaceId, FolderId = request.FolderId });
        var statuses = (await multi.ReadAsync<StatusRecord>()).AsList();
        var tasks = (await multi.ReadAsync<TaskRecord>()).AsList();
        var folders = new List<FolderRecord>();

        return Result<TaskViewData>.Success(new TaskViewData(folders, tasks, statuses));
    }
}


