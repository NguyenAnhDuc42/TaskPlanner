using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.CreateDashboard;

public class CreateDashboardHandler : BaseCommandHandler, IRequestHandler<CreateDashboardCommand, Unit>
{
    public CreateDashboardHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, permissionService, currentUserService, workspaceContext){}

    public async Task<Unit> Handle(CreateDashboardCommand request, CancellationToken cancellationToken)
    {
        var layer = await GetLayer(request.layerId, request.layerType);
        await RequirePermissionAsync(layer, EntityType.Dashboard, PermissionAction.Create, cancellationToken);
        
        var dashboard = Dashboard.CreateScopedDashboard(request.layerType, request.layerId, CurrentUserId, request.name, request.isShared, request.isMain);
        if (request.isMain) await ChangeMainDashboard(request.layerId,request.layerType,cancellationToken);

        await UnitOfWork.Set<Dashboard>().AddAsync(dashboard,cancellationToken);
        return Unit.Value;

    }

    private async Task ChangeMainDashboard(Guid layerId,EntityLayerType layerType,CancellationToken cancellation)
    {
        await UnitOfWork.Set<Dashboard>()
        .Where(d => d.LayerId == layerId && d.LayerType == layerType && d.IsMain)
        .ExecuteUpdateAsync(updater => updater
            .SetProperty(d => d.IsMain, _ => false)
            .SetProperty(d => d.UpdatedAt, _ => DateTime.UtcNow),
          cancellation);
    }

}

