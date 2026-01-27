using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;
using Application.Interfaces.Services.Permissions;
using Application.Common;
using Application.Helpers;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;

public class UpdateMembersHandler : BaseCommandHandler, IRequestHandler<UpdateMembersCommand, Unit>
{
    private readonly HybridCache _cache;

    public UpdateMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, HybridCache cache)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        // 1. Authorize & Fetch
        var workspace = await AuthorizeAndFetchAsync<ProjectWorkspace>(request.workspaceId, PermissionAction.Edit, cancellationToken);

        var updateDict = request.members.ToDictionary(x => x.userId);
        var userIdsToUpdate = updateDict.Keys.ToList();

        var membersToUpdate = await UnitOfWork.Set<WorkspaceMember>()
            .Where(m => m.ProjectWorkspaceId == request.workspaceId && userIdsToUpdate.Contains(m.UserId))
            .ToListAsync(cancellationToken);

        if (membersToUpdate.Count == 0) return Unit.Value;

        foreach (var member in membersToUpdate)
        {
            if (!updateDict.TryGetValue(member.UserId, out var updateInfo)) continue;

            if (member.Role == Role.Owner && updateInfo.role.HasValue && updateInfo.role.Value != Role.Owner)
            {
                if (updateInfo.status.HasValue) member.UpdateStatus(updateInfo.status.Value);
                continue;
            }

            if (updateInfo.role.HasValue) member.UpdateRole(updateInfo.role.Value);
            if (updateInfo.status.HasValue) member.UpdateStatus(updateInfo.status.Value);
        }

        await _cache.RemoveByTagAsync(CacheConstants.Tags.WorkspaceMembers(request.workspaceId), cancellationToken);

        return Unit.Value;
    }
}
