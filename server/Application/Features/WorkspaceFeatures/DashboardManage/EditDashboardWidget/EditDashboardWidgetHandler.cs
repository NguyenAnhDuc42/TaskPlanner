using System;
using System.Text.Json;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.DashboardManage.EditDashboardWidget;

public class EditDashboardWidgetHandler : BaseCommandHandler, IRequestHandler<EditDashboardWidgetCommand, Unit>
{
    public EditDashboardWidgetHandler
    (IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(EditDashboardWidgetCommand request, CancellationToken cancellationToken)
    {
        var widget = await UnitOfWork.Set<Widget>()
            .FindAsync(request.widgetId, cancellationToken)
            ?? throw new KeyNotFoundException("Widget not found");

        var dashboard = await UnitOfWork.Set<Dashboard>()
            .FindAsync(request.dashboardId, cancellationToken)
            ?? throw new KeyNotFoundException("Dashboard not found");

        await RequirePermissionAsync(dashboard, EntityType.Widget, PermissionAction.Edit, cancellationToken);

        var configJson = JsonSerializer.Serialize(request.filter);
        widget.UpdateConfig(configJson);

        UnitOfWork.Set<Widget>().Update(widget);

        return Unit.Value;
    }
}