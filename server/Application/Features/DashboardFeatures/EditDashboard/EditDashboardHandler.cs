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

namespace Application.Features.DashboardFeatures.EditDashboard;

[Obsolete("Dashboard features are legacy and will be removed in favor of modernized Functional Views.")]
public class EditDashboardHandler : BaseFeatureHandler, IRequestHandler<EditDashboardCommand, Unit>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public EditDashboardHandler(
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

    public async Task<Unit> Handle(EditDashboardCommand request, CancellationToken ct)
    {
        var dashboard = await GetDashboardAsync(request.dashboardId, ct);
        
        ApplyUpdates(dashboard, request);
        
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

    private void ApplyUpdates(Dashboard dashboard, EditDashboardCommand request)
    {
        dashboard.UpdateName(request.name);
        if (request.isShared.HasValue) dashboard.UpdateShared(request.isShared.Value);
        if (request.isMain.HasValue) dashboard.UpdateMain(request.isMain.Value);
    }

    private async Task SyncAndNotifyAsync(Dashboard dashboard, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(DashboardCacheKeys.DashboardListTag(dashboard.LayerId), ct);
        _ = _realtime.NotifyUserAsync(CurrentUserId, "DashboardUpdated", new { DashboardId = dashboard.Id }, ct);
    }
}