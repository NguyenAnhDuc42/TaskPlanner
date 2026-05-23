
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

        var parameters = new object[] {
            new Npgsql.NpgsqlParameter("WorkflowId", activeWorkflow.Id),
            new Npgsql.NpgsqlParameter("WorkspaceId", workspaceId),
            new Npgsql.NpgsqlParameter("SpaceId", request.SpaceId)
        };

        var statusesSql = @"
            SELECT id AS Id, id AS StatusId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
            FROM statuses
            WHERE workflow_id = @WorkflowId
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0
                WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2
                WHEN 'Closed' THEN 3
                ELSE 4
            END;";

        var foldersSql = @"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, status_id AS StatusId, priority AS Priority, start_date AS StartDate, due_date AS DueDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_folders
            WHERE project_space_id = @SpaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
            ORDER BY order_key;";

        var tasksSql = @"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, status_id AS StatusId, priority AS Priority, due_date AS DueDate, start_date AS StartDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_space_id = @SpaceId AND project_folder_id IS NULL
            ORDER BY order_key;";

        var statuses = await db.Database.SqlQueryRaw<StatusRecord>(statusesSql, parameters).ToListAsync(cancellationToken);
        var folders = await db.Database.SqlQueryRaw<FolderRecord>(foldersSql, parameters).ToListAsync(cancellationToken);
        var tasks = await db.Database.SqlQueryRaw<TaskRecord>(tasksSql, parameters).ToListAsync(cancellationToken);

        return Result<TaskViewData>.Success(new TaskViewData(folders, tasks, statuses));
    }
}


