using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.EntityMemberManagement.UpdateEntityMemberNotifications;

public class UpdateEntityMemberNotificationsHandler : BaseFeatureHandler, IRequestHandler<UpdateEntityMemberNotificationsCommand, Unit>
{
    public UpdateEntityMemberNotificationsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public Task<Unit> Handle(UpdateEntityMemberNotificationsCommand request, CancellationToken cancellationToken)
    {
        // PER USER REQUEST: Notifications are not supported in EntityAccess system.
        // This handler is now a no-op.
        return Task.FromResult(Unit.Value);
    }
}
