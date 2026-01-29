using Application.Common;
using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;
using Application.Helpers;

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public class RemoveMembersHandler : BaseFeatureHandler, IRequestHandler<RemoveMembersCommand, Guid>
{
    public RemoveMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
    }

    public async Task<Guid> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        // 1. Authorize & Fetch
        var workspace = await AuthorizeAndFetchAsync<ProjectWorkspace>(request.workspaceId, PermissionAction.Delete, cancellationToken);

        var members = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId))
            .ToListAsync(cancellationToken);

        if (members.Any())
        {
            workspace.RemoveMembers(members.Select(m => m.UserId));
            
            // Note: ProjectWorkspace.RemoveMembers raises WorkspaceMemberRemovedEvent
            // which handles the cache/SignalR plumbing.
        }

        return workspace.Id;
    }
}
