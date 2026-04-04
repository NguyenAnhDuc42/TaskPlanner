using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Application.Interfaces; 

using Microsoft.Extensions.Caching.Hybrid; 
using Microsoft.Extensions.Logging;
using Application.Features.WorkspaceFeatures.UpdateWorkspace;
using server.Application.Interfaces;


namespace Application.Features.WorkspaceFeatures.SelfManagement.UpdateWorkspace; 

public class UpdateWorkspaceHandler : BaseFeatureHandler, IRequestHandler<UpdateWorkspaceCommand, Unit>
{

    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;
    private readonly ILogger<UpdateWorkspaceHandler> _logger;

    public UpdateWorkspaceHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,

        HybridCache cache,
        IRealtimeService realtime,
        ILogger<UpdateWorkspaceHandler> logger)
        : base(unitOfWork, currentUserService, workspaceContext)
    {

        _cache = cache;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating workspace {WorkspaceId} by user {UserId}", request.Id, CurrentUserId);


        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.Id, cancellationToken);
        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.Id} not found");

        UpdateWorkspaceDetails(workspace, request);

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateCache(CurrentUserId, cancellationToken);

        NotifyClients(workspace.Id, CurrentUserId);

        return Unit.Value;
    }



    private void UpdateWorkspaceDetails(ProjectWorkspace workspace, UpdateWorkspaceCommand request)
    {
        // Update basic info
        if (request.Name is not null || request.Description is not null)
            workspace.UpdateBasicInfo(request.Name, request.Description);

        // Update customization
        if (request.Color is not null || request.Icon is not null)
            workspace.UpdateCustomization(request.Color, request.Icon);

        // Update settings
        if (request.Theme.HasValue)
            workspace.UpdateTheme(request.Theme.Value);

        if (request.StrictJoin.HasValue)
            workspace.UpdateStrictJoin(request.StrictJoin.Value);

        // Handle archive/unarchive
        if (request.IsArchived.HasValue)
        {
            if (request.IsArchived.Value) workspace.Archive();
            else workspace.Unarchive();
        }

        // Regenerate join code if requested
        if (request.RegenerateJoinCode)
            workspace.RegenerateJoinCode();
    }

    private async Task InvalidateCache(Guid userId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(userId), ct);
    }

    private void NotifyClients(Guid workspaceId, Guid userId)
    {
        _ = _realtime.NotifyUserAsync(userId, "WorkspaceUpdated", new { WorkspaceId = workspaceId }, default);
        _ = _realtime.NotifyWorkspaceAsync(workspaceId, "WorkspaceSettingsUpdated", new { WorkspaceId = workspaceId }, default);
    }
}
