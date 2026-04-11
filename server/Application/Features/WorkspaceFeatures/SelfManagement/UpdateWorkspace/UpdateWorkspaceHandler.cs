using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.UpdateWorkspace;

public class UpdateWorkspaceHandler : ICommandHandler<UpdateWorkspaceCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;
    private readonly ILogger<UpdateWorkspaceHandler> _logger;

    public UpdateWorkspaceHandler(IDataBase db, ICurrentUserService currentUserService, HybridCache cache, IRealtimeService realtime, ILogger<UpdateWorkspaceHandler> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _cache = cache;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        _logger.LogInformation("Updating workspace {WorkspaceId} by user {UserId}", request.Id, currentUserId);

        var workspace = await _db.Workspaces
            .ById(request.Id)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        // --- Apply Updates Inline ---
        if (request.Name is not null || request.Description is not null)
        {
            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;
            workspace.UpdateBasicInfo(request.Name, slug, request.Description);
        }

        if (request.Color is not null || request.Icon is not null) 
            workspace.UpdateCustomization(request.Color, request.Icon);

        if (request.Theme.HasValue) workspace.UpdateTheme(request.Theme.Value);
        if (request.StrictJoin.HasValue) workspace.UpdateStrictJoin(request.StrictJoin.Value);
        if (request.IsArchived.HasValue)
        {
            if (request.IsArchived.Value) workspace.Archive();
            else workspace.Unarchive();
        }
        if (request.RegenerateJoinCode) workspace.RegenerateJoinCode();

        await _db.SaveChangesAsync(cancellationToken);

        // --- Side Effects ---
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);
        
        _ = _realtime.NotifyUserAsync(currentUserId, "WorkspaceUpdated", new { WorkspaceId = workspace.Id }, default);
        _ = _realtime.NotifyWorkspaceAsync(workspace.Id, "WorkspaceSettingsUpdated", new { WorkspaceId = workspace.Id }, default);

        return Result.Success();
    }
}
