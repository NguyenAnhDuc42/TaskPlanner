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

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public class RemoveMembersHandler : BaseCommandHandler, IRequestHandler<RemoveMembersCommand, Unit>
{
    private readonly HybridCache _cache;

    public RemoveMembersHandler(
        IUnitOfWork unitOfWork, 
        IPermissionService permissionService, 
        ICurrentUserService currentUserService, 
        WorkspaceContext workspaceContext,
        HybridCache cache)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) 
    {
        _cache = cache;
    }

    public async Task<Unit> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId, cancellationToken) ?? throw new KeyNotFoundException("Workspace not found");

        await RequirePermissionAsync(workspace, EntityType.WorkspaceMember, PermissionAction.Delete, cancellationToken);
        await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId))
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // Invalidate cache
        await _cache.RemoveAsync(CacheKeys.WorkspaceMembers(request.workspaceId), cancellationToken);

        return Unit.Value;
    }
}
