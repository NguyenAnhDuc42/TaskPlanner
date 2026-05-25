using Microsoft.EntityFrameworkCore;
using Dapper;
namespace Application;

public class GetFolderDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderDetailQuery, FolderDetailResponse>
{
    public async Task<Result<FolderDetailResponse>> Handle(GetFolderDetailQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                f.id AS Id, 
                f.project_space_id AS ProjectSpaceId, 
                f.name AS Name, 
                f.custom_color AS Color, 
                f.custom_icon AS Icon,
                f.status_id AS StatusId, 
                f.priority AS Priority, 
                f.start_date AS StartDate, 
                f.due_date AS DueDate, 
                f.created_at AS CreatedAt,
                f.project_space_id AS ParentId,
                (SELECT wf.id FROM workflows wf WHERE wf.project_space_id = f.project_space_id AND wf.project_folder_id IS NULL LIMIT 1) AS ParentWorkflowId,
                (SELECT wf.id FROM workflows wf WHERE wf.project_folder_id = f.id LIMIT 1) AS WorkflowId
            FROM project_folders f
            WHERE f.id = @FolderId AND f.project_workspace_id = @WorkspaceId AND f.deleted_at IS NULL;";

        var connection = db.Database.GetDbConnection();
        var folderData = await connection.QueryFirstOrDefaultAsync<FolderQueryResult>(
            sql, new { FolderId = request.FolderId, WorkspaceId = workspaceContext.workspaceId });
        
        if (folderData == null)
            return Result<FolderDetailResponse>.Failure(Error.NotFound("Folder.NotFound", $"Folder {request.FolderId} not found"));

        var folderRecord = new FolderRecord
        {
            Id = folderData.Id,
            Name = folderData.Name,
            CreatedAt = folderData.CreatedAt,
            StatusId = folderData.StatusId,
            Priority = folderData.Priority,
            StartDate = folderData.StartDate,
            DueDate = folderData.DueDate,
            Icon = folderData.Icon,
            Color = folderData.Color,
            ParentId = folderData.ParentId
        };

        var activeWorkflowId = folderData.WorkflowId ?? folderData.ParentWorkflowId;
        List<StatusRecord> statuses = new();

        if (activeWorkflowId.HasValue)
        {
            var statusSql = @"
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
            
            statuses = (await connection.QueryAsync<StatusRecord>(statusSql, new { WorkflowId = activeWorkflowId })).AsList();
        }

        return Result<FolderDetailResponse>.Success(new FolderDetailResponse(folderRecord, statuses, activeWorkflowId));
    }

    private class FolderQueryResult
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? StatusId { get; set; }
        public Priority? Priority { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public Guid? ParentId { get; set; }
        public Guid? WorkflowId { get; set; }
        public Guid? ParentWorkflowId { get; set; }
    }
}
