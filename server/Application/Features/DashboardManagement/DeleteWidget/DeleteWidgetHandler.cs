using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.DeleteWidget;

public class DeleteWidgetHandler : BaseCommandHandler, IRequestHandler<DeleteWidgetCommand, Unit>
{
    public DeleteWidgetHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>().FindAsync(request.dashboardId, cancellationToken)
        ?? throw new KeyNotFoundException("Dashboard not found");

        await RequirePermissionAsync(dashboard, EntityType.Widget, PermissionAction.Delete, cancellationToken);

        dashboard.RemoveWidget(request.widgetId);

        UnitOfWork.Set<Dashboard>().Update(dashboard);

        return Unit.Value;

    }
}
