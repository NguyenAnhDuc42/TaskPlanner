using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Features.WorkspaceFeatures.DeleteWorkspace;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.DeleteWorkspace;

public class DeleteWorkspaceHandler : ICommandHandler<DeleteWorkspaceCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;
    private readonly ILogger<DeleteWorkspaceHandler> _logger;

    public DeleteWorkspaceHandler(IDataBase db, ICurrentUserService currentUserService, HybridCache cache, IRealtimeService realtime, ILogger<DeleteWorkspaceHandler> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _cache = cache;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteWorkspaceCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        _logger.LogInformation("Deleting workspace {WorkspaceId} by user {UserId}", request.workspaceId, currentUserId);

        var workspace = await _db.Workspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        workspace.SoftDelete();
        await _db.SaveChangesAsync(ct);

        // --- Side Effects ---
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);
        
        _ = _realtime.NotifyUserAsync(currentUserId, "WorkspaceDeleted", new { WorkspaceId = request.workspaceId }, default);
        _ = _realtime.NotifyWorkspaceAsync(request.workspaceId, "WorkspacePermanentlyDeleted", new { WorkspaceId = request.workspaceId }, default);

        return Result.Success();
    }
}
