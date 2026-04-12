using Application.Common.Interfaces;
using Application.Features.DashboardFeatures;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.Widget;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.CreateWidget;

[Obsolete("Dashboard features are legacy and will be removed in favor of modernized Functional Views.")]
public class CreateWidgetHandler : BaseFeatureHandler, IRequestHandler<CreateWidgetCommand, Guid>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public CreateWidgetHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService, 
        WorkspaceContext workspaceContext,
        HybridCache cache,
        IRealtimeService realtime)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _cache = cache;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(CreateWidgetCommand request, CancellationToken ct)
    {
        var dashboard = await GetDashboardAsync(request.dashboardId, ct);
        
        var widget = CreateWidget(dashboard, request); 
        
        await SyncAndNotifyAsync(widget, request.dashboardId, ct);
        
        return widget.Id;
    }

    private async Task<Dashboard> GetDashboardAsync(Guid dashboardId, CancellationToken ct)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .FirstOrDefaultAsync(d => d.Id == dashboardId && d.DeletedAt == null, ct);
            
        if (dashboard == null) throw new Exception("Dashboard not found.");
        return dashboard;
    }

    private Widget CreateWidget(Dashboard dashboard, CreateWidgetCommand request)
    {
        dashboard.AddWidget(
            widgetType: request.widgetType,
            configJson: "{}",
            col: request.Col,
            row: request.Row,
            width: request.Width,
            height: request.Height,
            creatorId: CurrentUserId
        );
        return dashboard.Widgets.Last();
    }

    private async Task SyncAndNotifyAsync(Widget widget, Guid dashboardId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WidgetCacheKeys.WidgetListTag(dashboardId), ct);
        _ = _realtime.NotifyUserAsync(CurrentUserId, "WidgetCreated", new { WidgetId = widget.Id, DashboardId = dashboardId }, ct);
    }
}
