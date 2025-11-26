using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.DeleteStatus;

public class DeleteStatusHandler : BaseCommandHandler, IRequestHandler<DeleteStatusCommand, Unit>
{
    public DeleteStatusHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await FindOrThrowAsync<Status>(request.StatusId);

        // TODO: Permission check based on status.LayerType and status.LayerId
        // TODO: Check if status is system status (future: prevent deletion of default statuses)
        
        UnitOfWork.Set<Status>().Remove(status);
        
        return Unit.Value;
    }
}
