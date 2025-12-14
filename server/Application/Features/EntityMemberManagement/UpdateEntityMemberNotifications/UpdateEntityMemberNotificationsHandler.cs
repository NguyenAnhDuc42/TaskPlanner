using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Relationship;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityMemberManagement.UpdateEntityMemberNotifications;

public class UpdateEntityMemberNotificationsHandler : BaseCommandHandler, IRequestHandler<UpdateEntityMemberNotificationsCommand, Unit>
{
    public UpdateEntityMemberNotificationsHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateEntityMemberNotificationsCommand request, CancellationToken cancellationToken)
    {
        // Find the current user's EntityMember record for this layer
        var entityMember = await UnitOfWork.Set<EntityMember>()
            .FirstOrDefaultAsync(em => em.LayerId == request.LayerId 
                                    && em.LayerType == request.LayerType 
                                    && em.UserId == CurrentUserId, 
                                 cancellationToken);

        if (entityMember == null)
            throw new KeyNotFoundException("You are not a member of this entity");

        // Update notifications setting - this is a personal setting, no permission check needed
        // User can only update their own notification preferences
        entityMember.UpdateNotificationsEnabled(request.NotificationsEnabled);

        return Unit.Value;
    }
}
