using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityMemberManagement.DeleteEntityMember;

public class DeleteEntityMemberHandler : BaseCommandHandler, IRequestHandler<DeleteEntityMemberCommand, Unit>
{
    public DeleteEntityMemberHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteEntityMemberCommand request, CancellationToken cancellationToken)
    {
        // Get parent layer
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Permission check
        await RequirePermissionAsync(layer, EntityType.EntityMember, PermissionAction.Delete, cancellationToken);

        // Find and remove members
        var membersToRemove = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == request.LayerId
                      && em.LayerType == request.LayerType
                      && request.UserIds.Contains(em.UserId))
            .ToListAsync(cancellationToken);

        if (membersToRemove.Any())
        {
            UnitOfWork.Set<EntityMember>().RemoveRange(membersToRemove);
        }

        return Unit.Value;
    }
}
