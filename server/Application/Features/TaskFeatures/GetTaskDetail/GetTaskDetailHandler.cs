using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Helpers;
using Dapper;

namespace Application.Features.TaskFeatures;

public class GetTaskDetailHandler(IDataBase db, WorkspaceContext workspaceContext) : IQueryHandler<GetTaskDetailQuery, TaskDetailDto>
{
    public async Task<Result<TaskDetailDto>> Handle(GetTaskDetailQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                t.id AS Id, t.project_space_id AS ProjectSpaceId, t.project_folder_id AS ProjectFolderId,
                t.name AS Name, t.custom_color AS Color, t.custom_icon AS Icon, 
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

        var task = await db.Connection.QuerySingleOrDefaultAsync<TaskDetailDto>(sql, new { 
            request.TaskId, 
            WorkspaceId = workspaceContext.workspaceId 
        });
        
        if (task == null)
            return Result<TaskDetailDto>.Failure(Error.NotFound("Task.NotFound", $"Task {request.TaskId} not found"));

        return Result<TaskDetailDto>.Success(task with { AssigneeIds = new List<Guid>() });
    }
}
