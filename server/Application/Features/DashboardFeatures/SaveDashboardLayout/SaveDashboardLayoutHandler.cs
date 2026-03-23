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

namespace Application.Features.DashboardFeatures.SaveDashboardLayout;

public class SaveDashboardLayoutHandler : BaseFeatureHandler, IRequestHandler<SaveDashboardLayoutCommand, bool>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public SaveDashboardLayoutHandler(
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

    public async Task<bool> Handle(SaveDashboardLayoutCommand request, CancellationToken ct)
    {
        var widgets = await GetWidgetsAsync(request, ct);
        
        if (widgets.Count == 0 && request.Layouts.Count > 0) return false;

        ApplyLayoutUpdates(widgets, request.Layouts);
        
        await SyncAndNotifyAsync(request.DashboardId, ct);
        
        return true;
    }

    private async Task<List<Widget>> GetWidgetsAsync(SaveDashboardLayoutCommand request, CancellationToken ct)
    {
        var widgetIds = request.Layouts.Select(l => l.WidgetId).ToList();
        return await UnitOfWork.Set<Widget>()
            .Where(w => w.DashboardId == request.DashboardId && widgetIds.Contains(w.Id) && w.DeletedAt == null)
            .ToListAsync(ct);
    }

    private void ApplyLayoutUpdates(List<Widget> widgets, List<WidgetLayoutUpdateDto> updates)
    {
        foreach (var update in updates)
        {
            var widget = widgets.FirstOrDefault(w => w.Id == update.WidgetId);
            if (widget != null)
            {
                var newLayout = new WidgetLayout(update.Col, update.Row, update.Width, update.Height);
                widget.UpdateLayout(newLayout);
            }
        }
    }

    private async Task SyncAndNotifyAsync(Guid dashboardId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WidgetCacheKeys.WidgetListTag(dashboardId), ct);
        _ = _realtime.NotifyUserAsync(CurrentUserId, "DashboardLayoutSaved", new { DashboardId = dashboardId }, ct);
    }
}
