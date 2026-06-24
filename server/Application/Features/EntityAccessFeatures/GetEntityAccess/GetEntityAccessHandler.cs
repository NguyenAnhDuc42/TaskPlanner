using Microsoft.EntityFrameworkCore;
using Dapper;


namespace Application;

public class GetEntityAccessHandler(TaskPlanDbContext db,WorkspaceContext workspaceContext) : IQueryHandler<GetEntityAccessQuery, IReadOnlyList<EntityAccessRecord>>, IAuthorizedWorkspaceRequest
{
    public async Task<Result<IReadOnlyList<EntityAccessRecord>>> Handle(GetEntityAccessQuery request, CancellationToken cancellationToken)
    {
        var query = """
            SELECT
                ea.id AS Id,
                @SpaceId AS SpaceId,
                wm.id AS WorkspaceMemberId,
                ea.access_level AS AccessLevel,
                ea.id IS NOT NULL AS HaveAccess
            FROM workspace_members wm
            LEFT JOIN entity_access ea ON ea.workspace_member_id = wm.id
                AND ea.project_space_id = @SpaceId
                AND ea.deleted_at IS NULL
            WHERE wm.project_workspace_id = @WorkspaceId AND wm.deleted_at IS NULL
        """;

        var connection = db.Database.GetDbConnection();
        var entityAccess = (await connection.QueryAsync<EntityAccessRecord>(
            query, new { WorkspaceId = workspaceContext.WorkspaceId, SpaceId = request.SpaceId })).AsList();

        return Result<IReadOnlyList<EntityAccessRecord>>.Success(entityAccess);
    }
}

