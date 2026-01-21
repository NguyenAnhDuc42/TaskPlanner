using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembersRole;

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


        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("No workspace founded");
        var membersToUpdate = await _unitOfWork.Set<WorkspaceMember>()
           .Where(m => m.ProjectWorkspaceId == request.workspaceId && userIdsToUpdate.Contains(m.UserId))
           .ToListAsync(cancellationToken);

        if (membersToUpdate.Count == 0) return Unit.Value; 
        foreach (var member in membersToUpdate)
        {
            if (!updateDict.TryGetValue(member.UserId, out var updateInfo)) continue;
            if (updateInfo.role.HasValue) member.UpdateRole(updateInfo.role.Value);
            if (updateInfo.status.HasValue) member.UpdateStatus(updateInfo.status.Value);
        }

        return Unit.Value;
    }
}
