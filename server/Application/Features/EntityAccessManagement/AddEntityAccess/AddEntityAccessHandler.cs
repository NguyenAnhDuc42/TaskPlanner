using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.EntityAccessManagement.AddEntityAccess;

public class AddEntityAccessHandler : BaseFeatureHandler, IRequestHandler<AddEntityAccessCommand, Unit>
{
    public CreateEntityMemberHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(AddEntityAccessCommand request, CancellationToken cancellationToken)
    {
        // Get parent layer (Space/Folder/List) using GetLayer helper
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Validate all users are workspace members
        var workspaceMembers = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => request.UserIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .ToListAsync(cancellationToken);

        if (workspaceMembers.Count != request.UserIds.Count)
            throw new ValidationException("One or more users are not members of this workspace");

        // Check for existing access
        var existingMemberIds = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == request.LayerId 
                      && ea.EntityLayer == request.LayerType 
                      && workspaceMembers.Select(wm => wm.Id).Contains(ea.WorkspaceMemberId))
            .Select(ea => ea.WorkspaceMemberId)
            .ToListAsync(cancellationToken);

        // Filter out users who already have access
        var newWorkspaceMemberIds = workspaceMembers
            .Where(wm => !existingMemberIds.Contains(wm.Id))
            .Select(wm => wm.Id)
            .ToList();

        if (!newWorkspaceMemberIds.Any())
            return Unit.Value;

        // Create new access records
        var newAccessRecords = newWorkspaceMemberIds
            .Select(memberId => EntityAccess.Create(memberId, request.LayerId, request.LayerType, request.AccessLevel, CurrentUserId))
            .ToList();

        await UnitOfWork.Set<EntityAccess>().AddRangeAsync(newAccessRecords, cancellationToken);

        return Unit.Value;
    }
}
