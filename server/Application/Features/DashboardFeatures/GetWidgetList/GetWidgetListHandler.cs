using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.GetWidgetList;

public class GetWidgetListHandler : BaseFeatureHandler, IRequestHandler<GetWidgetListQuery, List<WidgetDto>>
{
    public GetWidgetListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<WidgetDto>> Handle(GetWidgetListQuery request, CancellationToken cancellationToken)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.dashboardId, cancellationToken);

        if (dashboard == null) throw new KeyNotFoundException("Dashboard not found.");

        return dashboard.Widgets
            .Where(w => w.DeletedAt == null)
            .Select(w => new WidgetDto(
                w.Id,
                w.DashboardId,
                new WidgetLayoutDto(w.Layout.Col, w.Layout.Row, w.Layout.Width, w.Layout.Height),
                w.WidgetType,
                w.ConfigJson,
                w.Visibility))
            .ToList();
    }
}
