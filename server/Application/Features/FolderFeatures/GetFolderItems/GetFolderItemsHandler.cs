using Microsoft.EntityFrameworkCore;
namespace Application;

public class GetFolderItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderItemsQuery, TaskViewData>
{
    public async Task<Result<TaskViewData>> Handle(GetFolderItemsQuery request, CancellationToken ct)
    {
        var workspaceId = workspaceContext.workspaceId;

        // Get SpaceId for the folder to resolve workflow
        var spaceId = await db.Database.SqlQuery<Guid?>($"SELECT project_space_id FROM project_folders WHERE id = {request.FolderId} AND deleted_at IS NULL").FirstOrDefaultAsync(ct);

        if (spaceId == null)
            return Result<TaskViewData>.Failure(Error.NotFound("Folder.NotFound", "Folder not found"));

        var activeWorkflow = await WorkflowHelper.GetActiveWorkflow(db, workspaceId, 
            spaceId.Value, 
            request.FolderId, ct);

        if (activeWorkflow == null)
            return Result<TaskViewData>.Failure(Error.NotFound("Workflow.NotFound", "Active workflow not found for this folder"));

        // 1. Fetch Statuses
        var statuses = await db.Database.SqlQueryRaw<StatusRecord>(@"
            SELECT id AS Id, id AS StatusId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
            FROM statuses
            WHERE workflow_id = {0}
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0
                WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2
                WHEN 'Closed' THEN 3
                ELSE 4
            END", activeWorkflow.Id).ToListAsync(ct);

        // 2. Fetch Folders (Empty for folder level)
        var folders = new List<FolderRecord>();

        // 3. Fetch Tasks
        var tasks = await db.Database.SqlQueryRaw<TaskRecord>(@"
            SELECT id AS Id, name AS Name, created_at AS CreatedAt, status_id AS StatusId, priority AS Priority, due_date AS DueDate, start_date AS StartDate, order_key AS OrderKey, custom_icon as Icon, custom_color as Color
            FROM project_tasks
            WHERE project_workspace_id = {0} 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_folder_id = {1}
            ORDER BY order_key", workspaceId, request.FolderId).ToListAsync(ct);

        return Result<TaskViewData>.Success(new TaskViewData(folders, tasks, statuses));
    }
}


