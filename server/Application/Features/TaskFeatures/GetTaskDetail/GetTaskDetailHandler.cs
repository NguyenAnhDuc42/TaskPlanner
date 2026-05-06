using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Dapper;

using Application.Helpers;

namespace Application.Features.TaskFeatures;

public class GetTaskDetailHandler(IDataBase db, WorkspaceContext workspaceContext) : IQueryHandler<GetTaskDetailQuery, TaskDetailDto>
{
    public async Task<Result<TaskDetailDto>> Handle(GetTaskDetailQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                t.id AS Id, t.project_space_id AS ProjectSpaceId, 
                t.project_folder_id AS ProjectFolderId, t.name AS Name, 
                t.custom_color AS Color, t.custom_icon AS Icon, t.status_id AS StatusId, 
                t.is_archived AS IsArchived, t.priority AS Priority, 
                t.start_date AS StartDate, t.due_date AS DueDate, 
                t.story_points AS StoryPoints, 
                t.time_estimate_seconds AS TimeEstimateSeconds, 
                t.created_at AS CreatedAt,
                (SELECT b.content FROM document_blocks b WHERE b.document_id = t.default_document_id ORDER BY b.order_key LIMIT 1) AS Description
            FROM project_tasks t
            WHERE t.id = @TaskId AND t.project_workspace_id = @WorkspaceId AND t.deleted_at IS NULL;

            SELECT workspace_member_id
            FROM task_assignments
            WHERE project_task_id = @TaskId AND deleted_at IS NULL;";

        using var multi = await db.Connection.QueryMultipleAsync(sql, new { 
            request.TaskId, 
            WorkspaceId = workspaceContext.workspaceId 
        });
        
        var task = await multi.ReadSingleOrDefaultAsync<TaskDetailDto>();
        
        if (task == null)
            return Result<TaskDetailDto>.Failure(Error.NotFound("Task.NotFound", $"Task {request.TaskId} not found"));

        var assigneeIds = (await multi.ReadAsync<Guid>()).AsList();
        
        return Result<TaskDetailDto>.Success(task with { AssigneeIds = assigneeIds });
    }
}
