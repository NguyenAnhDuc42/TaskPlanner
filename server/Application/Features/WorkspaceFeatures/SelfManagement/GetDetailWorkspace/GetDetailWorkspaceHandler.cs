using Microsoft.EntityFrameworkCore;


namespace Application;

public class GetDetailWorkspaceHandler(TaskPlanDbContext db, WorkspaceContext context) : IQueryHandler<GetDetailWorkspaceQuery, WorkspaceRecord>
{
    public async Task<Result<WorkspaceRecord>> Handle(GetDetailWorkspaceQuery request, CancellationToken ct)
    {
        var parameters = new[]
        {
            new Npgsql.NpgsqlParameter("WorkspaceId", request.WorkspaceId),
            new Npgsql.NpgsqlParameter("UserId", context.CurrentMember.UserId)
        };

        var row = await db.Database.SqlQueryRaw<WorkspaceDetailRow>(
            GetDetailWorkspaceSQL.GetDetail, parameters).FirstOrDefaultAsync(ct);

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


