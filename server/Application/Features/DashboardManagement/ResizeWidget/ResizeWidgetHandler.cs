using System;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.ResizeWidget;

public class ResizeWidgetHandler : BaseFeatureHandler, IRequestHandler<ResizeWidgetCommand, Unit>
{
    public ResizeWidgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(ResizeWidgetCommand request, CancellationToken cancellationToken)
    {
        // Fetch dashboard aggregate
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);

        // Rebuild occupancy tracker from current widgets
        dashboard.RebuildOccupancyTracker();

        // Execute domain logic - ResizeWidget handles cascade internally
        try
        {
            dashboard.ResizeWidget(request.widgetId, request.newWidth, request.newHeight);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Cannot resize widget: {ex.Message}", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Resize operation failed: {ex.Message}", ex);
        }

        // Update aggregate in repository
        UnitOfWork.Set<Dashboard>().Update(dashboard);

        // Pipeline handles SaveChangesAsync with transaction + domain event dispatch
        return Unit.Value;
    }
}