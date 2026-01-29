using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.MoveWidget;

public class MoveWidgetHandler : BaseCommandHandler, IRequestHandler<MoveWidgetCommand, Unit>
{
    public MoveWidgetHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(MoveWidgetCommand request, CancellationToken cancellationToken)
    {
        // Fetch dashboard aggregate
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .FindAsync(request.dashboardId, cancellationToken)
            ?? throw new KeyNotFoundException("Dashboard not found");

        // Permission check
        await RequirePermissionAsync(dashboard, EntityType.Widget, PermissionAction.Edit, cancellationToken);

        // Rebuild occupancy tracker from current widgets
        dashboard.RebuildOccupancyTracker();

        // Execute domain logic - MoveWidget handles cascade internally
        try
        {
            dashboard.MoveWidget(request.widgetId, request.newCol, request.newRow);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Cannot move widget: {ex.Message}", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Move operation failed: {ex.Message}", ex);
        }

        // Update aggregate in repository
        UnitOfWork.Set<Dashboard>().Update(dashboard);

        // Pipeline handles SaveChangesAsync with transaction + domain event dispatch
        return Unit.Value;
    }
}