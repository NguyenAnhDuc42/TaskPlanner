using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetEntityAccessHandler(TaskPlanDbContext db,WorkspaceContext workspaceContext) : IQueryHandler<GetEntityAccessQuery, IReadOnlyList<EntityAccessDto>>, IAuthorizedWorkspaceRequest
{
    public async Task<Result<IReadOnlyList<EntityAccessDto>>> Handle(GetEntityAccessQuery request, CancellationToken cancellationToken)
    {   var connection = db.Database.GetDbConnection();
        var query = """
            SELECT 
                wm.id AS WorkspaceMemberId,
                ea.access_level AS AccessLevel,
                ea.workspace_member_id IS NOT NULL AS HaveAccess 
            FROM workspace_members wm
            LEFT JOIN entity_access ea ON ea.workspace_member_id = wm.id AND ea.project_space_id = @SpaceId
            Where wm.project_workspace_id = @WorkspaceId AND wm.deleted_on IS NULL
        """;

        var entityAccess = await connection.QueryAsync<EntityAccessDto>(query, new
        {
            WorkspaceId = workspaceContext.workspaceId,
            request.SpaceId
        });

        return Result<IReadOnlyList<EntityAccessDto>>.Success(entityAccess.ToList());
    }
}

