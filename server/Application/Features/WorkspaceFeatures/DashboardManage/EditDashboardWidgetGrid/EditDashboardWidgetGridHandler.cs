using System;
using System.ComponentModel.DataAnnotations;
using Application.Helpers.WidgetTool;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.DashboardManage.EditDashboardWidgetGrid;

public class EditDashboardWidgetGridHandler : BaseCommandHandler, IRequestHandler<EditDashboardWidgetGridCommand, Unit>
{
    private readonly WidgetGridValidator _widgetGridValidator;
    public EditDashboardWidgetGridHandler
    (IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, WidgetGridValidator widgetGridValidator)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
        _widgetGridValidator = widgetGridValidator;
    }

    public async Task<Unit> Handle(EditDashboardWidgetGridCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>()
        .Include(d => d.Widgets)
        .FirstOrDefaultAsync(d => d.Id == request.dashboardId, cancellationToken) 
        ?? throw new KeyNotFoundException("Dashboard not found.");

        await RequirePermissionAsync(dashboard, PermissionAction.Edit, cancellationToken);
        var validation = _widgetGridValidator.ValidateUpdates(dashboard, request.updateItems);
        if (!validation.IsValid) throw new ValidationException(validation.Errors.First());
        foreach (var update in request.updateItems)
        {
            dashboard.UpdateWidgetPosition(
                update.DashboardWidgetId,
                update.NewCol,
                update.NewRow,
                update.NewWidth,
                update.NewHeight
            );
        }
        return Unit.Value;
    }
}
