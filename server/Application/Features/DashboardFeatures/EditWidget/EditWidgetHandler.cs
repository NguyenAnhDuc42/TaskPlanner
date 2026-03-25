using Application.Common.Interfaces;
using Application.Features.DashboardFeatures;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.EditWidget;

public class EditWidgetHandler : BaseFeatureHandler, IRequestHandler<EditWidgetCommand, Unit>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public EditWidgetHandler(
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

    public async Task<Unit> Handle(EditWidgetCommand request, CancellationToken ct)
    {
        var (dashboard, widget) = await GetDashboardAndWidgetAsync(request, ct);
        
        ApplyUpdates(widget, request);
        
        await SyncAndNotifyAsync(widget, request.dashboardId, ct);
        
        return Unit.Value;
    }

    private async Task<(Dashboard, Widget)> GetDashboardAndWidgetAsync(EditWidgetCommand request, CancellationToken ct)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == request.dashboardId && d.DeletedAt == null, ct);

        if (dashboard == null) throw new KeyNotFoundException("Dashboard not found.");

        var widget = dashboard.Widgets.FirstOrDefault(w => w.Id == request.widgetId && w.DeletedAt == null);
        if (widget == null) throw new KeyNotFoundException("Widget not found.");
        
        return (dashboard, widget);
    }

    private void ApplyUpdates(Widget widget, EditWidgetCommand request)
    {
        if (request.configJson != null) widget.UpdateConfig(request.configJson);
    }

    private async Task SyncAndNotifyAsync(Widget widget, Guid dashboardId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WidgetCacheKeys.WidgetListTag(dashboardId), ct);
        _ = _realtime.NotifyUserAsync(CurrentUserId, "WidgetUpdated", new { WidgetId = widget.Id, DashboardId = dashboardId }, ct);
    }
}
