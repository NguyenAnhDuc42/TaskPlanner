using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Dapper;
using Domain.Entities;

using Application.Helpers;

namespace Application.Features.SpaceFeatures;

public class GetSpaceDetailHandler(IDataBase db, WorkspaceContext workspaceContext) : IQueryHandler<GetSpaceDetailQuery, SpaceDetailDto>
{
    public async Task<Result<SpaceDetailDto>> Handle(GetSpaceDetailQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                s.id AS Id, s.project_workspace_id AS ProjectWorkspaceId, s.name AS Name, 
                s.custom_color AS Color, s.custom_icon AS Icon, 
                s.is_private AS IsPrivate, s.is_archived AS IsArchived, 
                s.is_inheriting_workflow AS IsInheritingWorkflow,
                (SELECT wf.id FROM workflows wf WHERE wf.project_workspace_id = s.project_workspace_id AND wf.project_space_id IS NULL AND wf.project_folder_id IS NULL LIMIT 1) AS ParentWorkflowId,
                (SELECT wf.id FROM workflows wf WHERE wf.project_space_id = s.id AND wf.project_folder_id IS NULL LIMIT 1) AS WorkflowId,
                s.status_id AS StatusId, 
                s.default_document_id AS DefaultDocumentId, 
                s.start_date AS StartDate, s.due_date AS DueDate, s.created_at AS CreatedAt,
                (SELECT b.content FROM document_blocks b WHERE b.document_id = s.default_document_id ORDER BY b.order_key LIMIT 1) AS Description
            FROM project_spaces s
            WHERE s.id = @SpaceId AND s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL;";

        var space = await db.Connection.QuerySingleOrDefaultAsync<SpaceDetailDto>(sql, new { 
            request.SpaceId, 
            WorkspaceId = workspaceContext.workspaceId 
        });
        
        if (space == null)
            return Result<SpaceDetailDto>.Failure(Error.NotFound("Space.NotFound", $"Space {request.SpaceId} not found"));

        return Result<SpaceDetailDto>.Success(space with { MemberIds = new List<Guid>() });
    }
}
