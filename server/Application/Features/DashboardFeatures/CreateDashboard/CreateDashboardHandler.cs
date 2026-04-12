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
using Domain.Enums.RelationShip;

namespace Application.Features.DashboardFeatures.CreateDashboard;

[Obsolete("Dashboard features are legacy and will be removed in favor of modernized Functional Views.")]
public class CreateDashboardHandler : BaseFeatureHandler, IRequestHandler<CreateDashboardCommand, Guid>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public CreateDashboardHandler(
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

    public async Task<Guid> Handle(CreateDashboardCommand request, CancellationToken ct)
    {
        await ValidateAndLoadLayerAsync(request, ct);
        
        if (request.isMain)
        {
            await HandleMainDashboardTransitionAsync(request, ct);
        }

        var dashboard = await CreateAndAddDashboardAsync(request, ct);
        
        await SyncAndNotifyAsync(dashboard, request.layerId, ct);
        
        return dashboard.Id;
    }

    private async Task ValidateAndLoadLayerAsync(CreateDashboardCommand request, CancellationToken ct)
    {
        var layerExists = await UnitOfWork.Set<ProjectWorkspace>().AnyAsync(w => w.Id == request.layerId, ct);
        if (!layerExists) throw new Exception("Target layer not found.");
    }

    private async Task HandleMainDashboardTransitionAsync(CreateDashboardCommand request, CancellationToken ct)
    {
        await UnitOfWork.Set<Dashboard>()
            .Where(d => d.LayerId == request.layerId && d.LayerType == request.layerType && d.IsMain)
            .ExecuteUpdateAsync(updater => updater
                .SetProperty(d => d.IsMain, _ => false)
                .SetProperty(d => d.UpdatedAt, _ => DateTime.UtcNow),
              ct);
    }

    private async Task<Dashboard> CreateAndAddDashboardAsync(CreateDashboardCommand request, CancellationToken ct)
    {
        Dashboard dashboard = request.layerType == EntityLayerType.ProjectWorkspace
            ? Dashboard.CreateWorkspaceDashboard(request.layerId, CurrentUserId, request.name, request.isShared, request.isMain)
            : Dashboard.CreateScopedDashboard(request.layerType, request.layerId, CurrentUserId, request.name, request.isShared, request.isMain);

        await UnitOfWork.Set<Dashboard>().AddAsync(dashboard, ct);
        
        return dashboard;
    }

    private async Task SyncAndNotifyAsync(Dashboard dashboard, Guid layerId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(DashboardCacheKeys.DashboardListTag(layerId), ct);
        _ = _realtime.NotifyUserAsync(CurrentUserId, "DashboardCreated", new { DashboardId = dashboard.Id, LayerId = layerId }, ct);
    }
}
