using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class GetFavoritesHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFavoritesQuery, List<FavoriteRecord>>
{
    public async Task<Result<List<FavoriteRecord>>> Handle(GetFavoritesQuery request, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        const string sql = @"
            SELECT 
                id AS Id,
                entity_id AS EntityId,
                entity_layer_type AS EntityLayerType,
                order_key AS OrderKey,
                @WorkspaceId AS WorkspaceId
            FROM favorites
            WHERE workspace_member_id = @MemberId
            ORDER BY order_key;";

        var favorites = await connection.QueryAsync<FavoriteRecord>(sql, new { 
            WorkspaceId = workspaceContext.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id 
        });

        return Result<List<FavoriteRecord>>.Success(favorites.AsList());
    }
}
