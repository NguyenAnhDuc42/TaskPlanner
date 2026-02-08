using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccessNotifications;

public class UpdateEntityAccessNotificationsHandler : BaseFeatureHandler, IRequestHandler<UpdateEntityAccessNotificationsCommand, Unit>
{
    public UpdateEntityAccessNotificationsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public Task<Unit> Handle(UpdateEntityAccessNotificationsCommand request, CancellationToken cancellationToken)
    {
        // PER USER REQUEST: Notifications are not supported in EntityAccess system.
        // This handler is now a no-op.
        return Task.FromResult(Unit.Value);
    }
}
