using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;
using Application.Common;
using Application.Helpers;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;

public class UpdateMembersHandler : BaseFeatureHandler, IRequestHandler<UpdateMembersCommand, Unit>
{
    public UpdateMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.workspaceId);

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

        return Unit.Value;
    }
}
