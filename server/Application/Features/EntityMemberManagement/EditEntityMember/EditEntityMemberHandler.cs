using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityMemberManagement.EditEntityMember;

public class EditEntityMemberHandler : BaseCommandHandler, IRequestHandler<EditEntityMemberCommand, Unit>
{
    public EditEntityMemberHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditEntityMemberCommand request, CancellationToken cancellationToken)
    {
        // Get parent layer
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Permission check
        await RequirePermissionAsync(layer, EntityType.EntityMember, PermissionAction.Edit, cancellationToken);

        // Find and update members
        var membersToUpdate = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == request.LayerId
                      && em.LayerType == request.LayerType
                      && request.UserIds.Contains(em.UserId))
            .ToListAsync(cancellationToken);

        if (membersToUpdate.Any())
        {
            membersToUpdate.ForEach(em => em.UpdateAccessLevel(request.AccessLevel));
        }

        return Unit.Value;
    }
}
