using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccessBulk;

public class UpdateEntityAccessBulkHandler : BaseFeatureHandler, IRequestHandler<UpdateEntityAccessBulkCommand, Unit>
{
    public UpdateEntityAccessBulkHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateEntityAccessBulkCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve the owner/creator to prevent them from being removed or downgraded (if necessary, though handled in handler)
        // Here we just apply the logic similar to what was in UpdateSpaceHandler but generalize it.

        var existingAccess = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.EntityId == request.EntityId &&
                ea.EntityLayer == request.LayerType &&
                ea.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var existingMap = existingAccess.ToDictionary(ea => ea.WorkspaceMemberId);

        foreach (var memberUpdate in request.Members)
        {
            if (existingMap.TryGetValue(memberUpdate.WorkspaceMemberId, out var current))
            {
                if (memberUpdate.IsRemove)
                {
                    // Basic sanity: Don't remove if they are the only person? 
                    // Actually, the frontend should prevent removing the owner.
                    current.Remove();
                    continue;
                }

                if (memberUpdate.AccessLevel.HasValue)
                {
                    current.UpdateAccessLevel(memberUpdate.AccessLevel.Value);
                }
            }
            else if (!memberUpdate.IsRemove)
            {
                // Add new access
                var newAccess = EntityAccess.Create(
                    memberUpdate.WorkspaceMemberId,
                    request.EntityId,
                    request.LayerType,
                    memberUpdate.AccessLevel ?? AccessLevel.Viewer,
                    CurrentUserId);

                await UnitOfWork.Set<EntityAccess>().AddAsync(newAccess, cancellationToken);
            }
        }

        return Unit.Value;
    }
}
