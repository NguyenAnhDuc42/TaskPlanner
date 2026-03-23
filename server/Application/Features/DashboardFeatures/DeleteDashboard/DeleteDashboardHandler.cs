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

namespace Application.Features.DashboardFeatures.DeleteDashboard;

public class DeleteDashboardHandler : BaseFeatureHandler, IRequestHandler<DeleteDashboardCommand, Unit>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public DeleteDashboardHandler(
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

    public async Task<Unit> Handle(DeleteDashboardCommand request, CancellationToken ct)
    {
        var dashboard = await GetDashboardAsync(request.dashboardId, ct);
        
        ValidateDeletion(dashboard);
        
        dashboard.SoftDelete();
        
        await SyncAndNotifyAsync(dashboard, ct);
        
        return Unit.Value;
    }

    private async Task<Dashboard> GetDashboardAsync(Guid dashboardId, CancellationToken ct)
    {
        var dashboard = await UnitOfWork.Set<Dashboard>()
            .FirstOrDefaultAsync(d => d.Id == dashboardId && d.DeletedAt == null, ct);
            
        if (dashboard == null) throw new Exception("Dashboard not found.");
        return dashboard;
    }

    private void ValidateDeletion(Dashboard dashboard)
    {
        if (dashboard.IsMain) throw new InvalidOperationException("Cannot delete the main dashboard.");
    }

    private async Task SyncAndNotifyAsync(Dashboard dashboard, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(DashboardCacheKeys.DashboardListTag(dashboard.LayerId), ct);
        _ = _realtime.NotifyUserAsync(CurrentUserId, "DashboardDeleted", new { DashboardId = dashboard.Id }, ct);
    }
}
