using Dapper;

namespace Api;

public class WorkspaceDetailRow
{
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Role { get; init; } = null!;
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public string? JoinCode { get; init; }
    public bool StrictJoin { get; init; }
    public bool IsArchived { get; init; }
    public bool IsPinned { get; init; }
}

public class GetDetailWorkspaceHandler(TaskPlanDbContext db, WorkspaceContext context) : IQueryHandler<GetDetailWorkspaceQuery, WorkspaceRecord>
{
    public async Task<Result<WorkspaceRecord>> Handle(GetDetailWorkspaceQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                w.id AS WorkspaceId,
                w.name AS Name,
                w.description AS Description,
                wm.role AS Role,
                w.custom_color AS Color,
                w.custom_icon AS Icon,
                w.join_code AS JoinCode,
                w.strict_join AS StrictJoin,
                w.is_archived AS IsArchived,
                wm.is_pinned AS IsPinned
            FROM project_workspaces w
            JOIN workspace_members wm ON w.id = wm.project_workspace_id
            WHERE w.id = @WorkspaceId
              AND wm.user_id = @UserId
              AND w.deleted_at IS NULL
              AND wm.deleted_at IS NULL";

        var connection = db.Database.GetDbConnection();
        var row = await connection.QueryFirstOrDefaultAsync<WorkspaceDetailRow>(
            sql, new { WorkspaceId = request.WorkspaceId, UserId = context.CurrentMember.UserId });

        if (row == null)
            return Result<WorkspaceRecord>.Failure(WorkspaceError.NotFound);

        var isAdminOrOwner = row.Role == Role.Owner.ToString() || row.Role == Role.Admin.ToString();

        var dto = new WorkspaceRecord
        {
            Id = row.WorkspaceId,
            Name = row.Name,
            Description = row.Description,
            Role = Enum.TryParse<Role>(row.Role, out var roleEnum) ? roleEnum : null,
            Color = row.Color,
            Icon = row.Icon,
            JoinCode = isAdminOrOwner ? row.JoinCode : null,
            StrictJoin = row.StrictJoin,
            IsArchived = row.IsArchived,
            IsPinned = row.IsPinned
        };

        return Result<WorkspaceRecord>.Success(dto);
    }
}
