using Application.Helpers.WidgetTool;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Widget;
using Domain.Enums;
using Domain.Enums.Widget;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.CreateWidget;

public class CreateWidgetHandler : BaseCommandHandler, IRequestHandler<CreateWidgetCommand, Unit>
{
    public CreateWidgetHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(CreateWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>().FindAsync(request.dashboardId, cancellationToken) ?? throw new KeyNotFoundException("Dashboard not found");
        await RequirePermissionAsync(dashboard, EntityType.Widget, PermissionAction.Create, cancellationToken);

        var configJson = WidgetFactory.CreateWidgetConfig(request.widgetType, null);
        
        dashboard.AddWidget(
            widgetType: request.widgetType,
            configJson: configJson,
            visibility: WidgetVisibility.Public
        );
        
        UnitOfWork.Set<Dashboard>().Update(dashboard);
        
        return Unit.Value;
    }
}
