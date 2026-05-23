using Microsoft.EntityFrameworkCore;


namespace Application;

public class GetEntityAccessHandler(TaskPlanDbContext db,WorkspaceContext workspaceContext) : IQueryHandler<GetEntityAccessQuery, IReadOnlyList<EntityAccessRecord>>, IAuthorizedWorkspaceRequest
{
    public async Task<Result<IReadOnlyList<EntityAccessRecord>>> Handle(GetEntityAccessQuery request, CancellationToken cancellationToken)
    {
        var query = """
            SELECT 
                wm.id AS WorkspaceMemberId,
                ea.access_level AS AccessLevel,
                ea.workspace_member_id IS NOT NULL AS HaveAccess 
            FROM workspace_members wm
            LEFT JOIN entity_access ea ON ea.workspace_member_id = wm.id AND ea.project_space_id = @SpaceId
            Where wm.project_workspace_id = @WorkspaceId AND wm.deleted_on IS NULL
        """;

        var entityAccess = await db.Database.SqlQueryRaw<EntityAccessRecord>(query, 
            new Npgsql.NpgsqlParameter("WorkspaceId", workspaceContext.workspaceId),
            new Npgsql.NpgsqlParameter("SpaceId", request.SpaceId)
        ).ToListAsync(cancellationToken);

        return Result<IReadOnlyList<EntityAccessRecord>>.Success(entityAccess);
    }
}

