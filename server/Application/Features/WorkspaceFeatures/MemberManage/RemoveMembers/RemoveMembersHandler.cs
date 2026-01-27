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

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public class RemoveMembersHandler : BaseCommandHandler, IRequestHandler<RemoveMembersCommand, Guid>
{
    private readonly HybridCache _cache;

    public RemoveMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, HybridCache cache)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
        _cache = cache;
    }

    public async Task<Guid> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        // 1. Authorize & Fetch
        var workspace = await AuthorizeAndFetchAsync<ProjectWorkspace>(request.workspaceId, PermissionAction.Delete, cancellationToken);

        await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId))
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        await _cache.RemoveByTagAsync(CacheConstants.Tags.WorkspaceMembers(request.workspaceId), cancellationToken);

        return workspace.Id;
    }
}
