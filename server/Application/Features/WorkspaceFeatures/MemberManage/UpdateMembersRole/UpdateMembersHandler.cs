using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;

public class UpdateMembersHandler : IRequestHandler<UpdateMembersCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;

    public UpdateMembersHandler(IUnitOfWork unitOfWork,ICurrentUserService currentUserService,HybridCache cache)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var updateDict = request.members.ToDictionary(x => x.userId);

        var userIdsToUpdate = updateDict.Keys.ToList();
        var membersToUpdate = await _unitOfWork.Set<WorkspaceMember>()
            .Where(m => m.ProjectWorkspaceId == request.workspaceId && userIdsToUpdate.Contains(m.UserId))
            .ToListAsync(cancellationToken);

        if (membersToUpdate.Count == 0) return Unit.Value;

        foreach (var member in membersToUpdate)
        {
            if (!updateDict.TryGetValue(member.UserId, out var updateInfo)) continue;

            // Prevent changing the role of an Owner
            if (member.Role == Role.Owner && updateInfo.role.HasValue && updateInfo.role.Value != Role.Owner)
            {
                // We skip role updates for owners. Alternatively, we could throw an exception, 
                // but for batch updates, skipping is often more user-friendly.
                if (updateInfo.status.HasValue) member.UpdateStatus(updateInfo.status.Value);
                continue;
            }

            if (updateInfo.role.HasValue) member.UpdateRole(updateInfo.role.Value);

            if (updateInfo.status.HasValue) member.UpdateStatus(updateInfo.status.Value);
        }
        await _cache.RemoveByTagAsync($"workspaces:{request.workspaceId}:members", cancellationToken);

        return Unit.Value;
    }
}
