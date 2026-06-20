using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetSpaceDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceDetailQuery, SpaceRecord>
{
    public async Task<Result<SpaceRecord>> Handle(GetSpaceDetailQuery request, CancellationToken cancellationToken)
    {
        var isOwner = workspaceContext.CurrentMember.Role == Domain.Role.Owner;
        const string sql = @"
            SELECT 
                s.id AS Id, s.project_workspace_id AS ProjectWorkspaceId, s.name AS Name, 
                s.custom_color AS Color, s.custom_icon AS Icon, 
                s.is_private AS IsPrivate, s.is_archived AS IsArchived, 
                (SELECT wf.id FROM workflows wf WHERE wf.project_workspace_id = s.project_workspace_id AND wf.project_space_id IS NULL AND wf.project_folder_id IS NULL LIMIT 1) AS ParentWorkflowId,
                (SELECT wf.id FROM workflows wf WHERE wf.project_space_id = s.id AND wf.project_folder_id IS NULL LIMIT 1) AS WorkflowId,
                s.default_document_id AS DefaultDocumentId, 
                s.created_at AS CreatedAt,
                s.creator_id AS CreatorId,
                (SELECT b.content FROM document_blocks b WHERE b.document_id = s.default_document_id ORDER BY b.order_key LIMIT 1) AS Description,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_spaces s
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id 
                AND ea.workspace_member_id = @MemberId 
                AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = s.id AND fav.workspace_member_id = @MemberId
            WHERE s.id = @SpaceId AND s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
              AND (s.is_private = false OR ea.id IS NOT NULL OR @IsOwner = true);";

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            SpaceId = request.SpaceId,
            WorkspaceId = workspaceContext.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        };

        var row = await connection.QueryFirstOrDefaultAsync<SpaceRecord>(sql, parameters);
        
        if (row == null)
            return Result<SpaceRecord>.Failure(Error.NotFound("Space.NotFound", $"Space {request.SpaceId} not found"));

        var space = new SpaceRecord
        {
            Id = row.Id,
            WorkspaceId = workspaceContext.WorkspaceId,
            Name = row.Name,
            Color = row.Color,
            Icon = row.Icon,
            IsPrivate = row.IsPrivate,
            WorkflowId = row.WorkflowId,
            DefaultDocumentId = row.DefaultDocumentId,
            CreatedAt = row.CreatedAt,
            CreatorId = row.CreatorId,
            AccessLevel = row.AccessLevel,
            IsFavorite = row.IsFavorite
        };

        return Result<SpaceRecord>.Success(space);
    }
}


