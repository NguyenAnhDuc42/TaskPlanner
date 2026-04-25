using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application.Features.WorkspaceFeatures;

public class GetDetailWorkspaceHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto>
{
    public async Task<Result<WorkspaceSecurityContextDto>> Handle(GetDetailWorkspaceQuery request, CancellationToken ct)
    {
        var row = await db.Connection.QuerySingleOrDefaultAsync<WorkspaceDetailRow>(
            GetDetailWorkspaceSQL.GetDetail, 
            new { WorkspaceId = request.WorkspaceId, UserId = context.CurrentMember.UserId });

        if (row == null) 
            return Result<WorkspaceSecurityContextDto>.Failure(WorkspaceError.NotFound);

        var dto = new WorkspaceSecurityContextDto(
            WorkspaceId: row.WorkspaceId,
            CurrentRole: row.Role,
            IsOwned: row.Role == Role.Owner.ToString(),
            Theme: Enum.Parse<Theme>(row.Theme),
            Color: row.Color,
            Icon: row.Icon,
            CanEdit: row.Role == Role.Owner.ToString() || row.Role == Role.Admin.ToString(),
            CanInvite: row.Role == Role.Owner.ToString() || row.Role == Role.Admin.ToString(),
            CanManageMembers: row.Role == Role.Owner.ToString() || row.Role == Role.Admin.ToString(),
            IsDashboardEnabled: false
        );

        return Result<WorkspaceSecurityContextDto>.Success(dto);
    }
}
