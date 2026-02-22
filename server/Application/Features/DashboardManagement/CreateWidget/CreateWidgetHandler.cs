using Application.Helpers.WidgetTool;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.Widget;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.CreateWidget;

public class CreateWidgetHandler : BaseFeatureHandler, IRequestHandler<CreateWidgetCommand, Unit>
{
    public CreateWidgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(CreateWidgetCommand request, CancellationToken cancellationToken)
    {
        // Fetch dashboard aggregate
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);

        // Rebuild occupancy tracker from current widgets
        dashboard.RebuildOccupancyTracker();

        // Get default dimensions for widget type
        var (width, height) = WidgetFactory.GetDefaultWidgetDimensions(request.widgetType);

        // Create widget config
        var configJson = WidgetFactory.CreateWidgetConfig(request.widgetType, null);

        // Execute domain logic - AddWidget handles auto-placement internally
        try
        {
            dashboard.AddWidget(
                widgetType: request.widgetType,
                configJson: configJson,
                visibility: WidgetVisibility.Public,
                width: width,
                height: height,
                creatorId: CurrentUserId
            );
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Cannot create widget: {ex.Message}", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Widget creation failed: {ex.Message}", ex);
        }

        // Update aggregate in repository
        UnitOfWork.Set<Dashboard>().Update(dashboard);

        // Pipeline handles SaveChangesAsync with transaction + domain event dispatch
        return Unit.Value;
    }
}
