using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class SpaceDetailRow
{
    public Guid Id { get; init; }
    public Guid ProjectWorkspaceId { get; init; }
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool IsPrivate { get; init; }
    public bool IsArchived { get; init; }
    public Guid? ParentWorkflowId { get; init; }
    public Guid? WorkflowId { get; init; }
    public Guid? DefaultDocumentId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public string? Description { get; init; }
    public Guid? CreatorId { get; init; }
}

public class GetSpaceDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceDetailQuery, SpaceRecord>
{
    public async Task<Result<SpaceRecord>> Handle(GetSpaceDetailQuery request, CancellationToken ct)
    {
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
                (SELECT b.content FROM document_blocks b WHERE b.document_id = s.default_document_id ORDER BY b.order_key LIMIT 1) AS Description
            FROM project_spaces s
            WHERE s.id = @SpaceId AND s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL;";

        var connection = db.Database.GetDbConnection();
        var parameters = new
        {
            SpaceId = request.SpaceId,
            WorkspaceId = workspaceContext.workspaceId
        };

        var row = await connection.QueryFirstOrDefaultAsync<SpaceDetailRow>(sql, parameters);
        
        if (row == null)
            return Result<SpaceRecord>.Failure(Error.NotFound("Space.NotFound", $"Space {request.SpaceId} not found"));

        var space = new SpaceRecord
        {
            Id = row.Id,
            WorkspaceId = workspaceContext.workspaceId,
            Name = row.Name,
            Color = row.Color,
            Icon = row.Icon,
            IsPrivate = row.IsPrivate,
            ParentWorkflowId = row.ParentWorkflowId,
            WorkflowId = row.WorkflowId,
            DefaultDocumentId = row.DefaultDocumentId,
            CreatedAt = row.CreatedAt,
            Description = row.Description,
            CreatorId = row.CreatorId,
            MemberIds = new List<Guid>()
        };

        return Result<SpaceRecord>.Success(space);
    }
}


