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
        return await UnitOfWork.Set<Widget>()
            .Where(w => w.DashboardId == request.dashboardId && w.DeletedAt == null)
            .Select(w => new WidgetDto(
                w.Id,
                w.DashboardId,
                new WidgetLayoutDto(w.Layout.Col, w.Layout.Row, w.Layout.Width, w.Layout.Height),
                w.WidgetType,
                w.ConfigJson,
                w.Visibility))
            .ToListAsync(cancellationToken);
    }
}
