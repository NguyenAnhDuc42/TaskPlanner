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
        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("No workspace founded");
        var updateMember = workspace.Members
            .Where(m => request.members.Select(rm => rm.userId).Contains(m.UserId))
            .ToList();
        foreach (var member in updateMember)
        {

        }



        return Unit.Value;
    }
}
