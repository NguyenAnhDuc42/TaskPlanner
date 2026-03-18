using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.EditWidget;

public class EditWidgetHandler : BaseFeatureHandler, IRequestHandler<EditWidgetCommand, Unit>
{
    public EditWidgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.dashboardId, cancellationToken);

        if (dashboard == null) throw new KeyNotFoundException("Dashboard not found.");

        var widget = dashboard.Widgets.FirstOrDefault(w => w.Id == request.widgetId);
        if (widget == null) throw new KeyNotFoundException("Widget not found.");

        if (request.configJson != null) widget.UpdateConfig(request.configJson);
        if (request.visibility.HasValue) widget.UpdateVisibility(request.visibility.Value);

        return Unit.Value;
    }
}
