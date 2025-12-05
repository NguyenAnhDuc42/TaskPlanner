using System;
using Application.Common;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembersRole;

public class UpdateMembersHandler : BaseCommandHandler, IRequestHandler<UpdateMembersCommand, Unit>
{
    private readonly HybridCache _cache;

    public UpdateMembersHandler(
        IUnitOfWork unitOfWork, 
        IPermissionService permissionService, 
        ICurrentUserService currentUserService, 
        WorkspaceContext workspaceContext,
        HybridCache cache)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) 
    {
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>()
            .Where(w => w.Id == request.workspaceId)
            .FirstAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Workspace not found.");

        await RequirePermissionAsync(workspace, EntityType.WorkspaceMember, PermissionAction.Edit, cancellationToken);

        var userIdsToUpdate = request.members.Select(m => m.userId).ToList();
        var existingMembers = await UnitOfWork.Set<WorkspaceMember>()
             .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && userIdsToUpdate.Contains(wm.UserId))
             .ToListAsync(cancellationToken);
        
        foreach (var member in request.members)
        {
            var existingMember = existingMembers.FirstOrDefault(wm => wm.UserId == member.userId);
            if (existingMember != null)
            {
                existingMember.UpdateMembershipDetails(member.role, member.status);
            }
        }

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.WorkspaceMembers(request.workspaceId), cancellationToken);

        return Unit.Value;
    }
}
