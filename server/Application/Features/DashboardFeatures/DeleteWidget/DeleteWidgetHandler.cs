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

namespace Application.Features.DashboardFeatures.DeleteWidget;

public class DeleteWidgetHandler : BaseFeatureHandler, IRequestHandler<DeleteWidgetCommand, Unit>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public DeleteWidgetHandler(
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

    public async Task<Unit> Handle(DeleteWidgetCommand request, CancellationToken ct)
    {
        var dashboard = await GetDashboardAsync(request.dashboardId, ct);
        
        dashboard.RemoveWidget(request.widgetId);
        
        await SyncAndNotifyAsync(request.widgetId, request.dashboardId, ct);
        
        return Unit.Value;
    }

    private async Task<Dashboard> GetDashboardAsync(Guid dashboardId, CancellationToken ct)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == dashboardId && d.DeletedAt == null, ct);
            
        if (dashboard == null) throw new Exception("Dashboard not found.");
        return dashboard;
    }

    private async Task SyncAndNotifyAsync(Guid widgetId, Guid dashboardId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WidgetCacheKeys.WidgetListTag(dashboardId), ct);
        _ = _realtime.NotifyUserAsync(CurrentUserId, "WidgetDeleted", new { WidgetId = widgetId, DashboardId = dashboardId }, ct);
    }
}
