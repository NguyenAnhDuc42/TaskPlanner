using Application.Helpers;
using Domain.Entities.ProjectEntities;
using MediatR;
using Application.Interfaces; 

using Microsoft.Extensions.Caching.Hybrid; 
using Microsoft.Extensions.Logging; 
using Application.Features.WorkspaceFeatures.DeleteWorkspace;
using Application.Interfaces.Repositories;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.DeleteWorkspace; 

public class DeleteWorkspaceHandler : BaseFeatureHandler, IRequestHandler<DeleteWorkspaceCommand, Unit>
{

    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;
    private readonly ILogger<DeleteWorkspaceHandler> _logger;

    public DeleteWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, HybridCache cache, IRealtimeService realtime, ILogger<DeleteWorkspaceHandler> logger)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _cache = cache;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting workspace {WorkspaceId} by user {UserId}", request.workspaceId, CurrentUserId);
        await PerformDelete(request.workspaceId, cancellationToken);
        await InvalidateCache(CurrentUserId, cancellationToken);
        NotifyClients(request.workspaceId, CurrentUserId);
        return Unit.Value;
    }



    private async Task PerformDelete(Guid workspaceId, CancellationToken ct)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(workspaceId);
        if (workspace == null) throw new KeyNotFoundException($"Workspace {workspaceId} not found");
        workspace.SoftDelete();
        await UnitOfWork.SaveChangesAsync(ct);
    }

    private async Task InvalidateCache(Guid userId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(userId), ct);
    }

    private void NotifyClients(Guid workspaceId, Guid userId)
    {
        _ = _realtime.NotifyUserAsync(userId, "WorkspaceDeleted", new { WorkspaceId = workspaceId }, default);
        _ = _realtime.NotifyWorkspaceAsync(workspaceId, "WorkspacePermanentlyDeleted", new { WorkspaceId = workspaceId }, default);
    }
}
