using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccess;

public class UpdateEntityAccessHandler : BaseFeatureHandler, IRequestHandler<UpdateEntityAccessCommand, Unit>
{
    public EditEntityMemberHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateEntityAccessCommand request, CancellationToken cancellationToken)
    {
        // Get parent layer
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Resolve workspace member IDs
        var workspaceMemberIds = await GetWorkspaceMemberIds(request.UserIds, cancellationToken);

        // Find and update access records
        var accessToUpdate = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == request.LayerId
                      && ea.EntityLayer == request.LayerType
                      && workspaceMemberIds.Contains(ea.WorkspaceMemberId))
            .ToListAsync(cancellationToken);

        if (accessToUpdate.Any())
        {
            accessToUpdate.ForEach(ea => ea.UpdateAccessLevel(request.AccessLevel));
        }

        return Unit.Value;
    }
}
