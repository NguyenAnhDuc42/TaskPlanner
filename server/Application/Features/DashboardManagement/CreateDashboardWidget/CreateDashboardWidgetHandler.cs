using Application.Helpers.WidgetTool;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;


namespace Application.Features.DashboardManagement.CreateDashboardWidget;
public class CreateDashboardWidgetHandler : BaseCommandHandler, IRequestHandler<CreateDashboardWidgetCommand, Unit>
{
    private readonly WidgetFatory _widgetFactory;

    public CreateDashboardWidgetHandler
    (IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, WidgetFatory widgetFactory)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
        _widgetFactory = widgetFactory;
    }

    public async Task<Unit> Handle(CreateDashboardWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>().FindAsync(request.dashboardId, cancellationToken) ?? throw new KeyNotFoundException("Dashboard not found");
        await RequirePermissionAsync(dashboard, EntityType.Widget, PermissionAction.Create, cancellationToken);
        var widget = _widgetFactory.CreateWidget(
        type: request.widgetType,
            layerType: dashboard.LayerType,  // Pass it
            layerId: dashboard.LayerId,
            userId: CurrentUserId,
            filter: null
        );
        await UnitOfWork.Set<Widget>().AddAsync(widget, cancellationToken);
        dashboard.AddWidget(widget.Id);
        UnitOfWork.Set<Dashboard>().Update(dashboard);
        return Unit.Value;
    }
}
