using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures;

public class GetDetailWorkspaceHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto>
{
    public async Task<Result<WorkspaceSecurityContextDto>> Handle(GetDetailWorkspaceQuery request, CancellationToken ct)
    {
        // PermissionDecorator guarantees context.CurrentMember is set
        var workspace = await db.Workspaces
            .AsNoTracking()
            .ById(context.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result<WorkspaceSecurityContextDto>.Failure(WorkspaceError.NotFound);

        var role = context.CurrentMember.Role;

        var dto = new WorkspaceSecurityContextDto(
            WorkspaceId: context.workspaceId,
            CurrentRole: role.ToString(),
            IsOwned: role == Role.Owner,
            Theme: context.CurrentMember.Theme,
            Color: workspace.Customization.Color,
            Icon: workspace.Customization.Icon,
            CanEdit: role == Role.Owner || role == Role.Admin,
            CanInvite: role == Role.Owner || role == Role.Admin,
            CanManageMembers: role == Role.Owner || role == Role.Admin,
            IsDashboardEnabled: false
        );

        return Result<WorkspaceSecurityContextDto>.Success(dto);
    }
}
