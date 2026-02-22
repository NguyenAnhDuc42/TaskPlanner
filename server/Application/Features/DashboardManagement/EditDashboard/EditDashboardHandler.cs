using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.EditDashboard;


public class EditDashboardHandler : BaseFeatureHandler, IRequestHandler<EditDashboardCommand, Unit>
{
    public EditDashboardHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditDashboardCommand request, CancellationToken cancellationToken)
    {
        // Fetch dashboard
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);

        // Update properties if provided
        if (!string.IsNullOrWhiteSpace(request.name))
        {
            dashboard.UpdateName(request.name);
        }

        if (request.isShared.HasValue)
        {
            dashboard.UpdateShared(request.isShared.Value);
        }

        if (request.isMain.HasValue && request.isMain.Value)
        {
            // If setting as main, unset other main dashboards in same layer
            await ChangeMainDashboard(dashboard.LayerId, dashboard.LayerType, cancellationToken);
            dashboard.UpdateMain(true);
        }

        // Update aggregate
        UnitOfWork.Set<Dashboard>().Update(dashboard);

        // Pipeline handles SaveChangesAsync with transaction
        return Unit.Value;
    }

    private async Task ChangeMainDashboard(Guid layerId, EntityLayerType layerType, CancellationToken cancellation)
    {
        await UnitOfWork.Set<Dashboard>()
            .Where(d => d.LayerId == layerId && d.LayerType == layerType && d.IsMain)
            .ExecuteUpdateAsync(updater => updater
                .SetProperty(d => d.IsMain, _ => false)
                .SetProperty(d => d.UpdatedAt, _ => DateTime.UtcNow),
            cancellation);
    }
}