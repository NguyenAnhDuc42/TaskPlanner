using Microsoft.EntityFrameworkCore;
using Dapper;


namespace Application;

public class WorkspaceDetailRow
{
    public Guid WorkspaceId { get; init; }
    public string Role { get; init; } = null!;
    public string Theme { get; init; } = null!;
    public string? Color { get; init; }
    public string? Icon { get; init; }
}

public class GetDetailWorkspaceHandler(TaskPlanDbContext db, WorkspaceContext context) : IQueryHandler<GetDetailWorkspaceQuery, WorkspaceRecord>
{
    public async Task<Result<WorkspaceRecord>> Handle(GetDetailWorkspaceQuery request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                w.id AS WorkspaceId,
                wm.role AS Role,
                wm.theme AS Theme,
                w.custom_color AS Color,
                w.custom_icon AS Icon
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

        var dto = new WorkspaceRecord
        {
            Id = row.WorkspaceId,
            Role = Enum.TryParse<Role>(row.Role, out var roleEnum) ? roleEnum : null,
            IsOwned = row.Role == Role.Owner.ToString(),
            Theme = Enum.TryParse<Theme>(row.Theme, out var themeEnum) ? themeEnum : Theme.Dark,
            Color = row.Color,
            Icon = row.Icon,
            CanEdit = row.Role == Role.Owner.ToString() || row.Role == Role.Admin.ToString(),
            CanInvite = row.Role == Role.Owner.ToString() || row.Role == Role.Admin.ToString(),
            CanManageMembers = row.Role == Role.Owner.ToString() || row.Role == Role.Admin.ToString(),
            IsDashboardEnabled = false
        };

        return Result<WorkspaceRecord>.Success(dto);
    }
}


