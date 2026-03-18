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
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.DashboardId, cancellationToken);

        if (dashboard == null) return false;

        foreach (var update in request.Layouts)
        {
            var widget = dashboard.Widgets.FirstOrDefault(w => w.Id == update.WidgetId);
            if (widget != null)
            {
                var newLayout = new WidgetLayout(update.Col, update.Row, update.Width, update.Height);
                widget.UpdateLayout(newLayout);
            }
        }

        return true;
    }
}
