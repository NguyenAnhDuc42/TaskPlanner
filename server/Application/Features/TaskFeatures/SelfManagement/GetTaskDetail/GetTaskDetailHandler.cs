using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetTaskDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetTaskDetailQuery, List<TaskRecord>>
{
    public async Task<Result<List<TaskRecord>>> Handle(GetTaskDetailQuery request, CancellationToken cancellationToken)
    {
        var isOwner = workspaceContext.CurrentMember.Role == Domain.Role.Owner;
        const string sql = @"
            SELECT 
                t.id AS Id, t.project_space_id AS SpaceId, t.project_folder_id AS FolderId,
                t.name AS Name, t.custom_color AS Color, t.custom_icon AS Icon, 
                t.default_document_id AS DefaultDocumentId,
                t.is_archived AS IsArchived, t.priority AS Priority, 
                t.story_points AS StoryPoints, t.time_estimate_seconds AS TimeEstimateSeconds,
                t.status_id AS StatusId,
                t.start_date AS StartDate, t.due_date AS DueDate, t.created_at AS CreatedAt,
                (SELECT b.content FROM document_blocks b WHERE b.document_id = t.default_document_id ORDER BY b.order_key LIMIT 1) AS Description,
                t.parent_task_id AS ParentTaskId,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_tasks t
            INNER JOIN project_spaces s ON s.id = t.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id 
                AND ea.workspace_member_id = @MemberId 
                AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = t.id AND fav.workspace_member_id = @MemberId
            WHERE t.id = @TaskId AND t.project_workspace_id = @WorkspaceId AND t.deleted_at IS NULL
              AND (s.is_private = false OR (ea.id IS NOT NULL AND ea.access_level IN ('Viewer', 'Editor', 'Manager')) OR @IsOwner = true);";

        var connection = db.Database.GetDbConnection();
        var parameters = new {
            TaskId = request.TaskId,
            WorkspaceId = workspaceContext.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        };

        var task = await connection.QueryFirstOrDefaultAsync<TaskRecord>(sql, parameters);
        
        if (task == null)
            return Result<List<TaskRecord>>.Failure(Error.NotFound("Task.NotFound", $"Task {request.TaskId} not found"));

        const string subtasksSql = @"
            SELECT 
                t.id AS Id, t.project_space_id AS SpaceId, t.project_folder_id AS FolderId,
                t.name AS Name, t.custom_color AS Color, t.custom_icon AS Icon, 
                t.default_document_id AS DefaultDocumentId,
                t.is_archived AS IsArchived, t.priority AS Priority, 
                t.story_points AS StoryPoints, t.time_estimate_seconds AS TimeEstimateSeconds,
                t.status_id AS StatusId, 
                t.start_date AS StartDate, t.due_date AS DueDate, t.created_at AS CreatedAt,
                t.parent_task_id AS ParentTaskId,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_tasks t
            INNER JOIN project_spaces s ON s.id = t.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id 
                AND ea.workspace_member_id = @MemberId 
                AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = t.id AND fav.workspace_member_id = @MemberId
            WHERE t.parent_task_id = @TaskId AND t.project_workspace_id = @WorkspaceId AND t.deleted_at IS NULL
              AND (s.is_private = false OR (ea.id IS NOT NULL AND ea.access_level IN ('Viewer', 'Editor', 'Manager')) OR @IsOwner = true);";

        var subtasks = await connection.QueryAsync<TaskRecord>(subtasksSql, parameters);

        var list = new List<TaskRecord> { task };
        list.AddRange(subtasks);

        return Result<List<TaskRecord>>.Success(list);
    }
}
