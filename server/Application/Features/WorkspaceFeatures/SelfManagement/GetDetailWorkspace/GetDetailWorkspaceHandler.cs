using Application.Contract.UserContract;
using Application.Contract.WorkspaceContract;
using Application.Helper;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public class GetDetailWorkspaceHandler : BaseQueryHandler, IRequestHandler<GetDetailWorkspaceQuery, WorkspaceDetailDto>
{
    public GetDetailWorkspaceHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext, cursorHelper)
    {
    }

    public async Task<WorkspaceDetailDto> Handle(GetDetailWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var workspace = await QueryNoTracking<ProjectWorkspace>()
            .Where(w => w.Id == request.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        // Check if user has access
        await RequirePermissionAsync(workspace, PermissionAction.View, cancellationToken);

        // Get current user's membership info
        var currentMember = await QueryNoTracking<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == workspace.Id && wm.UserId == CurrentUserId)
            .Select(wm => new MemberDto
            {
                Id = wm.UserId,
                Role = wm.Role
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("User is not a member of this workspace");

        return new WorkspaceDetailDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description ?? string.Empty,
            Color = workspace.Customization.Color,
            Icon = workspace.Customization.Icon,
            Variant = workspace.Variant.ToString(),
            JoinCode = workspace.JoinCode,
            IsOwned = workspace.CreatorId == CurrentUserId,
            CurrentRole = currentMember,
            Permissions = new List<string>(), // TODO: Implement permission list when ready
            Settings = new WorkspaceSettingsDto
            {
                Theme = workspace.Theme,
                Color = workspace.Customization.Color,
                Icon = workspace.Customization.Icon,
                StrictJoin = workspace.StrictJoin
            }
        };
    }
}
