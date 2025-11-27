using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.ReorderStatus;

public class ReorderStatusHandler : BaseCommandHandler, IRequestHandler<ReorderStatusCommand, Unit>
{
    public ReorderStatusHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(ReorderStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await FindOrThrowAsync<Status>(request.StatusId);

        // Get the layer entity to check permissions
        var layerEntity = await GetLayer(status.LayerId!.Value, status.LayerType);
        
        // Require permission to edit Status on this layer (reordering is an edit operation)
        await RequirePermissionAsync(layerEntity, EntityType.Status, PermissionAction.Edit, cancellationToken);
        
        status.UpdateOrderKey(request.NewOrderKey);
        
        return Unit.Value;
    }
}
