using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support;
using Domain.Enums;
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

        var layerEntity = await GetLayer(status.LayerId!.Value, status.LayerType);
        await RequirePermissionAsync(layerEntity,status, PermissionAction.Delete, cancellationToken);
        status.SoftDelete();
        
        return Unit.Value;
    }
}
