using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Application.Features.ViewFeatures;

namespace Application.Features.FolderFeatures;

public class GetFolderItemsHandler(IDataBase db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderItemsQuery, TaskViewData>
{
    public async Task<Result<TaskViewData>> Handle(GetFolderItemsQuery request, CancellationToken ct)
    {
        var workspaceId = workspaceContext.workspaceId;

        // Get SpaceId for the folder to resolve workflow
        var spaceId = await db.Connection.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT project_space_id FROM project_folders WHERE id = @FolderId AND deleted_at IS NULL", 
            new { request.FolderId });

        if (spaceId == null)
            return Result<TaskViewData>.Failure(Application.Common.Errors.Error.NotFound("Folder.NotFound", "Folder not found"));

        var activeWorkflow = await WorkflowHelper.GetActiveWorkflow(db, workspaceId, 
            spaceId.Value, 
            request.FolderId, ct);

        if (activeWorkflow == null)
            return Result<TaskViewData>.Failure(Application.Common.Errors.Error.NotFound("Workflow.NotFound", "Active workflow not found for this folder"));

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

            -- 2. Fetch Folders (Empty for folder level)
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, start_date AS StartDate, due_date AS DueDate, order_key AS OrderKey
            FROM project_folders
            WHERE 1=0;

            -- 3. Fetch Tasks
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, priority, due_date AS DueDate, start_date AS StartDate, order_key AS OrderKey
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_folder_id = @FolderId
            ORDER BY order_key;";

        var parameters = new {
            WorkflowId = activeWorkflow.Id,
            WorkspaceId = workspaceId,
            FolderId = request.FolderId
        };

        using var multi = await db.Connection.QueryMultipleAsync(sql, parameters);
        
        var statuses = (await multi.ReadAsync<TaskItemStatusDto>()).ToList();
        var folders = (await multi.ReadAsync<FolderItemDto>()).ToList();
        var tasks = (await multi.ReadAsync<TaskItemDto>()).ToList();

        return Result<TaskViewData>.Success(new TaskViewData(folders, tasks, statuses));
    }
}
