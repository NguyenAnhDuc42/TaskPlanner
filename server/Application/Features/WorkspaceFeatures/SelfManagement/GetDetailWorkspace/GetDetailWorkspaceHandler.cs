using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public class GetDetailWorkspaceHandler : IQueryHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public GetDetailWorkspaceHandler(
        IDataBase db,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WorkspaceSecurityContextDto>> Handle(GetDetailWorkspaceQuery request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure<WorkspaceSecurityContextDto>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workspace = await _db.Workspaces
            .AsNoTracking()
            .ById(request.WorkspaceId)
            .FirstOrDefaultAsync(ct);
            
        if (workspace == null) return Result.Failure<WorkspaceSecurityContextDto>(WorkspaceError.NotFound);

        var memberValue = await _db.Members
            .AsNoTracking()
            .ByWorkspace(request.WorkspaceId)
            .ByUser(currentUserId)
            .FirstOrDefaultAsync(ct);

        var role = memberValue?.Role ?? Role.Guest;

        var dto = new WorkspaceSecurityContextDto
        {
            WorkspaceId = request.WorkspaceId,
            CurrentRole = role.ToString(),
            IsOwned = role == Role.Owner,
            Theme = workspace.Theme,
            Color = workspace.Customization.Color,
            Icon = workspace.Customization.Icon,
            
            // Permissions mapping
            CanEdit = role == Role.Owner || role == Role.Admin,
            CanInvite = role == Role.Owner || role == Role.Admin,
            CanManageMembers = role == Role.Owner || role == Role.Admin,
            
            // Feature Toggles (Frontend control)
            IsDashboardEnabled = false // Disabled per user request
        };

        return Result.Success(dto);
    }
}
