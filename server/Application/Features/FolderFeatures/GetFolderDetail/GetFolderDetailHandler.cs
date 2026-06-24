using Microsoft.EntityFrameworkCore;
using Dapper;
namespace Application;

public class GetFolderDetailHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : IQueryHandler<GetFolderDetailQuery, FolderDetailResponse>
{
    public async Task<Result<FolderDetailResponse>> Handle(GetFolderDetailQuery request, CancellationToken cancellationToken)
    {
        var isOwner = workspaceContext.CurrentMember.Role == Domain.Role.Owner;
        const string sql = @"
            SELECT
                f.id AS Id, f.project_space_id AS SpaceId, f.name AS Name,
                f.custom_icon AS Icon, f.custom_color AS Color,
                f.start_date AS StartDate, f.due_date AS DueDate,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_folders f
            INNER JOIN project_spaces s ON s.id = f.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = f.id AND fav.workspace_member_id = @MemberId
            WHERE f.id = @FolderId AND f.project_workspace_id = @WorkspaceId AND f.deleted_at IS NULL
              AND (s.is_private = false OR (ea.id IS NOT NULL AND ea.access_level IN ('Viewer', 'Editor', 'Manager')) OR @IsOwner = true);

            SELECT s.name AS Name, s.custom_icon AS Icon, s.custom_color AS Color
            FROM project_spaces s
            INNER JOIN project_folders f ON f.project_space_id = s.id
            WHERE f.id = @FolderId AND f.project_workspace_id = @WorkspaceId LIMIT 1;

            SELECT id AS Id, project_space_id AS SpaceId, name AS Name, color AS Color, category AS Category, order_key AS OrderKey
            FROM statuses
            WHERE project_space_id = (
                SELECT project_space_id FROM project_folders WHERE id = @FolderId LIMIT 1
            )
            ORDER BY CASE category
                WHEN 'NotStarted' THEN 0 WHEN 'Active' THEN 1
                WHEN 'Done' THEN 2 WHEN 'Closed' THEN 3 ELSE 4 END;";

        var connection = db.Database.GetDbConnection();
        using var multi = await connection.QueryMultipleAsync(sql, new {
            FolderId = request.FolderId,
            WorkspaceId = workspaceContext.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        });

        var folder = await multi.ReadFirstOrDefaultAsync<FolderRecord>();
        if (folder == null) return Result<FolderDetailResponse>.Failure(Error.NotFound("Folder.NotFound", $"Folder {request.FolderId} not found"));

        var space = await multi.ReadFirstOrDefaultAsync<BreadcrumbInfo>();
        var spaceStatuses = (await multi.ReadAsync<StatusRecord>()).AsList();

        return Result<FolderDetailResponse>.Success(new FolderDetailResponse(
            folder,
            space ?? new BreadcrumbInfo("", null, null),
            spaceStatuses
        ));
    }
}
