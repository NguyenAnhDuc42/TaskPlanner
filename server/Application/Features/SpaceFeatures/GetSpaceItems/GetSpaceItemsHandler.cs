using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Application.Features.ViewFeatures;

namespace Application.Features.SpaceFeatures;

public class GetSpaceItemsHandler(IDataBase db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceItemsQuery, TaskViewData>
{
    public async Task<Result<TaskViewData>> Handle(GetSpaceItemsQuery request, CancellationToken ct)
    {
        var workspaceId = workspaceContext.workspaceId;

        var activeWorkflow = await WorkflowHelper.GetActiveWorkflow(db, workspaceId, 
            request.SpaceId, 
            null, ct);

        if (activeWorkflow == null)
            return Result<TaskViewData>.Failure(Application.Common.Errors.Error.NotFound("Workflow.NotFound", "Active workflow not found for this space"));

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
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, start_date AS StartDate, due_date AS DueDate
            FROM project_folders
            WHERE project_space_id = @SpaceId 
              AND deleted_at IS NULL 
              AND is_archived = false;

            -- 3. Fetch Tasks
            SELECT id, name, created_at AS CreatedAt, status_id AS StatusId, priority, due_date AS DueDate, start_date AS StartDate
            FROM project_tasks
            WHERE project_workspace_id = @WorkspaceId 
              AND deleted_at IS NULL 
              AND is_archived = false
              AND project_space_id = @SpaceId AND project_folder_id IS NULL;";

        var parameters = new {
            WorkflowId = activeWorkflow.Id,
            WorkspaceId = workspaceId,
            SpaceId = request.SpaceId
        };

        using var multi = await db.Connection.QueryMultipleAsync(sql, parameters);
        
        var statuses = (await multi.ReadAsync<TaskItemStatusDto>()).ToList();
        var folders = (await multi.ReadAsync<FolderItemDto>()).ToList();
        var tasks = (await multi.ReadAsync<TaskItemDto>()).ToList();

        return Result<TaskViewData>.Success(new TaskViewData(folders, tasks, statuses));
    }
}
