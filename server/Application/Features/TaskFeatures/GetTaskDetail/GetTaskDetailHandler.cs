using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class TaskDetailRow
{
    public Guid Id { get; init; }
    public Guid? ProjectSpaceId { get; init; }
    public Guid? ProjectFolderId { get; init; }
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public Guid? DefaultDocumentId { get; init; }
    public bool IsArchived { get; init; }
    public Priority? Priority { get; init; }
    public int? StoryPoints { get; init; }
    public int? TimeEstimateSeconds { get; init; }
    public Guid? StatusId { get; init; }
    public Guid? ParentWorkflowId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? Description { get; init; }
}

public class GetTaskDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetTaskDetailQuery, TaskRecord>
{
    public async Task<Result<TaskRecord>> Handle(GetTaskDetailQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                t.id AS Id, t.project_space_id AS ProjectSpaceId, t.project_folder_id AS ProjectFolderId,
                t.name AS Name, t.custom_color AS Color, t.custom_icon AS Icon, 
                t.default_document_id AS DefaultDocumentId,
                t.is_archived AS IsArchived, t.priority AS Priority, 
                t.story_points AS StoryPoints, t.time_estimate_seconds AS TimeEstimateSeconds,
                t.status_id AS StatusId, 
                (
                    SELECT wf.id FROM workflows wf 
                    WHERE (t.project_folder_id IS NOT NULL AND wf.project_folder_id = t.project_folder_id)
                       OR (t.project_folder_id IS NULL AND wf.project_space_id = t.project_space_id AND wf.project_folder_id IS NULL)
                       OR (wf.project_workspace_id = t.project_workspace_id AND wf.project_space_id IS NULL AND wf.project_folder_id IS NULL)
                    ORDER BY 
                        CASE 
                            WHEN wf.project_folder_id = t.project_folder_id THEN 1
                            WHEN wf.project_space_id = t.project_space_id THEN 2
                            ELSE 3 
                        END
                    LIMIT 1
                ) AS ParentWorkflowId,
                t.start_date AS StartDate, t.due_date AS DueDate, t.created_at AS CreatedAt,
                (SELECT b.content FROM document_blocks b WHERE b.document_id = t.default_document_id ORDER BY b.order_key LIMIT 1) AS Description
            FROM project_tasks t
            WHERE t.id = @TaskId AND t.project_workspace_id = @WorkspaceId AND t.deleted_at IS NULL;";

        var connection = db.Database.GetDbConnection();
        var parameters = new {
            TaskId = request.TaskId,
            WorkspaceId = workspaceContext.workspaceId
        };

        var row = await connection.QueryFirstOrDefaultAsync<TaskDetailRow>(sql, parameters);
        
        if (row == null)
            return Result<TaskRecord>.Failure(Error.NotFound("Task.NotFound", $"Task {request.TaskId} not found"));

        var task = new TaskRecord
        {
            Id = row.Id,
            ProjectSpaceId = row.ProjectSpaceId,
            ProjectFolderId = row.ProjectFolderId,
            Name = row.Name,
            Color = row.Color,
            Icon = row.Icon,
            DefaultDocumentId = row.DefaultDocumentId,
            IsArchived = row.IsArchived,
            Priority = row.Priority,
            StoryPoints = row.StoryPoints,
            TimeEstimateSeconds = row.TimeEstimateSeconds,
            StatusId = row.StatusId,
            ParentWorkflowId = row.ParentWorkflowId,
            StartDate = row.StartDate,
            DueDate = row.DueDate,
            CreatedAt = row.CreatedAt,
            Description = row.Description,
            AssigneeIds = new List<Guid>()
        };

        return Result<TaskRecord>.Success(task);
    }
}


