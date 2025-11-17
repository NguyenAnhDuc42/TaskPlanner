using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.DashboardManagement.DeleteDashboard;

public class DeleteDashboadHandler : BaseCommandHandler, IRequestHandler<DeleteDashboardCommand, Unit>
{
    public DeleteDashboadHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, permissionService, currentUserService, workspaceContext){}

    public async Task<Unit> Handle(DeleteDashboardCommand request, CancellationToken cancellationToken)
    {
        var layer = await GetLayer(request.layerId, request.layerType);
        await RequirePermissionAsync(layer, EntityType.Dashboard, PermissionAction.Delete, cancellationToken);
        var dashboard = await UnitOfWork.Set<Dashboard>().FindAsync(request.dashboardId) ?? throw new KeyNotFoundException("No dashboard founded");
        if (dashboard.IsMain == true) throw new InvalidOperationException("Cannot delete the main dashboard. Unset IsMain first or use a force-delete flow.");

        UnitOfWork.Set<Dashboard>().Remove(dashboard);
        return Unit.Value;

    }
}
