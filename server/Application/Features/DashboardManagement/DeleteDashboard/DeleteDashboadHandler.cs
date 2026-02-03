using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.DashboardManagement.DeleteDashboard;

public class DeleteDashboadHandler : BaseFeatureHandler, IRequestHandler<DeleteDashboardCommand, Unit>
{
    public DeleteDashboadHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, currentUserService, workspaceContext){}

    public async Task<Unit> Handle(DeleteDashboardCommand request, CancellationToken cancellationToken)
    {
        var layer = await GetLayer(request.layerId, request.layerType);
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);
        if (dashboard.IsMain == true) throw new InvalidOperationException("Cannot delete the main dashboard. Unset IsMain first or use a force-delete flow.");

        dashboard.SoftDelete();
        return Unit.Value;

    }
}
