using Microsoft.EntityFrameworkCore;
using Dapper;
namespace Application;

public class GetFolderDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderDetailQuery, FolderDetailResponse>
{
    public async Task<Result<FolderDetailResponse>> Handle(GetFolderDetailQuery request, CancellationToken cancellationToken)
    {
        var isOwner = workspaceContext.CurrentMember.Role == Domain.Role.Owner;
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
                f.due_date    AS DueDate,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_folders f
            INNER JOIN project_spaces s ON s.id = f.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id 
                AND ea.workspace_member_id = @MemberId 
                AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = f.id AND fav.workspace_member_id = @MemberId
            WHERE f.id = @FolderId AND f.project_workspace_id = @WorkspaceId AND f.deleted_at IS NULL
              AND (s.is_private = false OR ea.id IS NOT NULL OR @IsOwner = true);

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
        using var multi = await connection.QueryMultipleAsync(sql, new { 
            FolderId = request.FolderId, 
            WorkspaceId = workspaceContext.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        });

        var folder = await multi.ReadFirstOrDefaultAsync<FolderRecord>();
        if (folder == null) return Result<FolderDetailResponse>.Failure(Error.NotFound("Folder.NotFound", $"Folder {request.FolderId} not found"));

        var space = await multi.ReadFirstOrDefaultAsync<BreadcrumbInfo>();
        var parentWorkflowId = await multi.ReadFirstOrDefaultAsync<Guid?>();
        var workflowId = await multi.ReadFirstOrDefaultAsync<Guid?>();

        folder = folder with { WorkflowId = workflowId };

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

        List<StatusRecord> taskStatuses;
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
