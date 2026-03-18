using Application.Common.Interfaces;
using Domain.Entities.ProjectEntities;
using Domain.Enums.Widget;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardFeatures.CreateWidget;

public record class CreateWidgetCommand(
    Guid dashboardId, 
    WidgetType widgetType,
    int Col,
    int Row,
    int Width,
    int Height) : IRequest<Unit>;

public class CreateWidgetHandler : BaseFeatureHandler<CreateWidgetCommand, Unit>
{
    public CreateWidgetHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService) { }

    public async Task<Unit> Handle(CreateWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await _context.Dashboards
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.dashboardId, cancellationToken);

        if (dashboard == null)
            throw new InvalidOperationException("Dashboard not found.");

        // Create widget config (using 1x1 as placeholder if factory isn't updated)
        // Ideally the dashboard.AddWidget is now the active domain logic.
        
        dashboard.AddWidget(
            widgetType: request.widgetType,
            configJson: "{}", // Default empty config
            visibility: WidgetVisibility.Public,
            col: request.Col,
            row: request.Row,
            width: request.Width,
            height: request.Height,
            creatorId: CurrentUserId
        );

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
