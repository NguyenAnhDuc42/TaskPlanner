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

public class RemoveMembersHandler : IRequestHandler<RemoveMembersCommand, Guid>
{
    private readonly HybridCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveMembersHandler(HybridCache cache,IUnitOfWork unitOfWork )
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Set<ProjectWorkspace>().AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("No workspace founded");

        await _unitOfWork.Set<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId))
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // Invalidate cache
        await  _cache.RemoveByTagAsync($"workspaces:{request.workspaceId}:members", cancellationToken);

        return workspace.Id;
    }
}
