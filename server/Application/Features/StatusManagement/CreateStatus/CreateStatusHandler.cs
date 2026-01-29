using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Support;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.CreateStatus;

public class CreateStatusHandler : BaseCommandHandler, IRequestHandler<CreateStatusCommand, Guid>
{
    public CreateStatusHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateStatusCommand request, CancellationToken cancellationToken)
    {
        var layerEntity = await GetLayer(request.LayerId, request.LayerType);

        await RequirePermissionAsync(layerEntity, EntityType.Status, PermissionAction.Create, cancellationToken);
        
        var orderKey = request.OrderKey ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var status = Status.Create(
            layerId: request.LayerId,
            layerType: request.LayerType,
            name: request.Name,
            color: request.Color,
            category: request.Category,
            orderKey: orderKey,
            creatorId: CurrentUserId
        );

        await UnitOfWork.Set<Status>().AddAsync(status, cancellationToken);
        
        return status.Id;
    }
}
