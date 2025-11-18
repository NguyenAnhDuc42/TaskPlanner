using System;
using System.Text.Json;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.EditWidget;

public class EditWidgetHandler : BaseCommandHandler, IRequestHandler<EditWidgetCommand, Unit>
{
    public EditWidgetHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditWidgetCommand request, CancellationToken cancellationToken)
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
