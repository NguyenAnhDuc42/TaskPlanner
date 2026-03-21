using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.SaveDashboardLayout;

public class SaveDashboardLayoutHandler : BaseFeatureHandler, IRequestHandler<SaveDashboardLayoutCommand, bool>
{
    public SaveDashboardLayoutHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<bool> Handle(SaveDashboardLayoutCommand request, CancellationToken cancellationToken)
    {
        var widgetIds = request.Layouts.Select(l => l.WidgetId).ToList();
        var widgets = await UnitOfWork.Set<Widget>()
            .Where(w => w.DashboardId == request.DashboardId && widgetIds.Contains(w.Id))
            .ToListAsync(cancellationToken);

        if (widgets.Count == 0 && request.Layouts.Count > 0) return false;

        foreach (var update in request.Layouts)
        {
            var widget = widgets.FirstOrDefault(w => w.Id == update.WidgetId);
            if (widget != null)
            {
                var newLayout = new WidgetLayout(update.Col, update.Row, update.Width, update.Height);
                widget.UpdateLayout(newLayout);
            }
        }

        return true;
    }
}
