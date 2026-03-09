using Application.Helpers;
using Domain.Entities.ProjectEntities;
using MediatR;
using Application.Interfaces; 
using Application.Features.WorkspaceFeatures.Logic;
using Microsoft.Extensions.Caching.Hybrid; 
using Microsoft.Extensions.Logging; 
using Application.Features.WorkspaceFeatures.DeleteWorkspace;
using Application.Interfaces.Repositories;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.DeleteWorkspace; 

public class DeleteWorkspaceHandler : BaseFeatureHandler, IRequestHandler<DeleteWorkspaceCommand, Unit>
{
    private readonly WorkspacePermissionLogic _workspacePermissionLogic;
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;
    private readonly ILogger<DeleteWorkspaceHandler> _logger;

    public DeleteWorkspaceHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        WorkspacePermissionLogic workspacePermissionLogic,
        HybridCache cache,
        IRealtimeService realtime,
        ILogger<DeleteWorkspaceHandler> logger)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _workspacePermissionLogic = workspacePermissionLogic;
        _cache = cache;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting workspace {WorkspaceId} by user {UserId}", request.workspaceId, CurrentUserId);

        await ValidatePermission(request.workspaceId, CurrentUserId, cancellationToken);
        
        await PerformDelete(request.workspaceId, cancellationToken);
        
        await InvalidateCache(CurrentUserId, cancellationToken);
        
        NotifyClients(request.workspaceId, CurrentUserId);

        return Unit.Value;
    }

    private async Task ValidatePermission(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        await _workspacePermissionLogic.EnsureCanDeleteWorkspace(workspaceId, userId, ct);
    }

    private async Task PerformDelete(Guid workspaceId, CancellationToken ct)
    {
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(workspaceId);
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
