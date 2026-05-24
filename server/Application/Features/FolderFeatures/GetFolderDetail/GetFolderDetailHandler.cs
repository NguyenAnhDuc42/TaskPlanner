using Microsoft.EntityFrameworkCore;
using Dapper;
namespace Application;

public class GetFolderDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderDetailQuery, FolderDetailDto>
{
    public async Task<Result<FolderDetailDto>> Handle(GetFolderDetailQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                f.id AS Id, f.project_space_id AS ProjectSpaceId, f.name AS Name, 
                f.custom_color AS Color, f.custom_icon AS Icon, f.is_private AS IsPrivate, 
                f.is_archived AS IsArchived, 

                (SELECT wf.id FROM workflows wf WHERE wf.project_space_id = f.project_space_id AND wf.project_folder_id IS NULL LIMIT 1) AS ParentWorkflowId,
                (SELECT wf.id FROM workflows wf WHERE wf.project_folder_id = f.id LIMIT 1) AS WorkflowId,
                f.status_id AS StatusId, 
                f.priority AS Priority, 
                f.default_document_id AS DefaultDocumentId, 
                f.start_date AS StartDate, f.due_date AS DueDate, f.created_at AS CreatedAt,
                (SELECT b.content FROM document_blocks b WHERE b.document_id = f.default_document_id ORDER BY b.order_key LIMIT 1) AS Description
            FROM project_folders f
            WHERE f.id = @FolderId AND f.project_workspace_id = @WorkspaceId AND f.deleted_at IS NULL;";

        var connection = db.Database.GetDbConnection();
        var folder = await connection.QueryFirstOrDefaultAsync<FolderDetailDto>(
            sql, new { FolderId = request.FolderId, WorkspaceId = workspaceContext.workspaceId });
        
        if (folder == null)
            return Result<FolderDetailDto>.Failure(Error.NotFound("Folder.NotFound", $"Folder {request.FolderId} not found"));

        return Result<FolderDetailDto>.Success(folder with { MemberIds = new List<Guid>() });
    }
}


