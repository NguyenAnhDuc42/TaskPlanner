using Application.Contract.UserContract;
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

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public class GetMembersHandler : BaseQueryHandler, IRequestHandler<GetMembersQuery, List<MemberDto>>
{
    public GetMembersHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext, cursorHelper)
    {
    }

    public async Task<List<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var workspace = await QueryNoTracking<ProjectWorkspace>()
            .Where(w => w.Id == request.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        // Check access
        await RequirePermissionAsync(workspace, EntityType.WorkspaceMember, PermissionAction.View, cancellationToken);

        // Get all members
        var members = await QueryNoTracking<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId && wm.DeletedAt == null)
            .Select(wm => new MemberDto
            {
                Id = wm.UserId,
                Role = wm.Role
            })
            .ToListAsync(cancellationToken);

        return members;
    }
}
