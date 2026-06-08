using Microsoft.EntityFrameworkCore;
using Dapper;
namespace Application;

public class GetFolderDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderDetailQuery, FolderDetailResponse>
{
    public async Task<Result<FolderDetailResponse>> Handle(GetFolderDetailQuery request, CancellationToken ct)
    {
        // Only select columns shown on the folder view page
        const string sql = @"
            SELECT 
                f.id          AS Id,
                f.project_space_id AS SpaceId,
                f.name        AS Name,
                f.custom_icon AS Icon,
                f.custom_color AS Color,
                f.status_id   AS StatusId,
                f.priority    AS Priority,
                f.start_date  AS StartDate,
                f.due_date    AS DueDate
            FROM project_folders f
            WHERE f.id = @FolderId AND f.project_workspace_id = @WorkspaceId AND f.deleted_at IS NULL;

            SELECT s.name AS Name, s.custom_icon AS Icon, s.custom_color AS Color
            FROM project_spaces s
            INNER JOIN project_folders f ON f.project_space_id = s.id
            WHERE f.id = @FolderId AND f.project_workspace_id = @WorkspaceId LIMIT 1;

            SELECT wf.id FROM workflows wf
            INNER JOIN project_folders f ON f.project_space_id = wf.project_space_id
            WHERE f.id = @FolderId AND wf.project_folder_id IS NULL LIMIT 1;

            SELECT wf.id FROM workflows wf
            WHERE wf.project_folder_id = @FolderId LIMIT 1;";

        var connection = db.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(
            sql, new { FolderId = request.FolderId, WorkspaceId = workspaceContext.WorkspaceId });

        var folder = await multi.ReadFirstOrDefaultAsync<FolderRecord>();
        if (folder == null)
            return Result<FolderDetailResponse>.Failure(Error.NotFound("Folder.NotFound", $"Folder {request.FolderId} not found"));

        var space = await multi.ReadFirstOrDefaultAsync<BreadcrumbInfo>();
        var parentWorkflowId = await multi.ReadFirstOrDefaultAsync<Guid?>();
        var workflowId = await multi.ReadFirstOrDefaultAsync<Guid?>();

        List<StatusRecord> spaceStatuses = new();
        if (parentWorkflowId.HasValue)
        {
            var statusSql = @"
                SELECT id AS Id, id AS StatusId, workflow_id AS WorkflowId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
                FROM statuses
                WHERE workflow_id = @ParentWorkflowId
                ORDER BY CASE category
                    WHEN 'NotStarted' THEN 0
                    WHEN 'Active' THEN 1
                    WHEN 'Done' THEN 2
                    WHEN 'Closed' THEN 3
                    ELSE 4
                END;";

            spaceStatuses = (await connection.QueryAsync<StatusRecord>(statusSql, new {
                ParentWorkflowId = parentWorkflowId
            })).AsList();
        }

        List<StatusRecord> taskStatuses = new();
        if (workflowId.HasValue)
        {
            var taskStatusSql = @"
                SELECT id AS Id, id AS StatusId, workflow_id AS WorkflowId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
                FROM statuses
                WHERE workflow_id = @WorkflowId
                ORDER BY CASE category
                    WHEN 'NotStarted' THEN 0
                    WHEN 'Active' THEN 1
                    WHEN 'Done' THEN 2
                    WHEN 'Closed' THEN 3
                    ELSE 4
                END;";

            taskStatuses = (await connection.QueryAsync<StatusRecord>(taskStatusSql, new {
                WorkflowId = workflowId
            })).AsList();
        }
        else
        {
            // Folder has no custom workflow — tasks inherit space statuses
            taskStatuses = spaceStatuses;
        }

        StatusRecord? folderStatus = null;
        if (folder.StatusId.HasValue)
        {
            folderStatus = spaceStatuses.FirstOrDefault(s => s.Id == folder.StatusId.Value);

            if (folderStatus == null)
            {
                folderStatus = await connection.QueryFirstOrDefaultAsync<StatusRecord>(@"
                    SELECT id AS Id, id AS StatusId, workflow_id AS WorkflowId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
                    FROM statuses WHERE id = @StatusId;",
                    new { StatusId = folder.StatusId.Value });
            }
        }

        return Result<FolderDetailResponse>.Success(new FolderDetailResponse(
            folder,
            space ?? new BreadcrumbInfo("", null, null),
            folderStatus,
            parentWorkflowId,
            spaceStatuses,
            workflowId,
            taskStatuses
        ));
    }
}
