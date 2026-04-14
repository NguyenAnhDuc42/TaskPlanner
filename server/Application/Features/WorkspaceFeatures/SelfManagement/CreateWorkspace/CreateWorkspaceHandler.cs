using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces;
using Application.Features;

namespace Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;

public class CreateWorkspaceHandler(
    IDataBase db, 
    ICurrentUserService currentUserService, 
    HybridCache cache, 
    IRealtimeService realtime,
    IBackgroundJobService backgroundJob
) : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result<Guid>.Failure(UserError.NotFound);

        var workspace = ProjectWorkspace.Create(
            name: request.Name,
            slug: SlugHelper.GenerateSlug(request.Name),
            description: request.Description,
            joinCode: null,
            customization: Customization.Create(request.Color, request.Icon),
            creatorId: currentUserId,
            theme: request.Theme,
            strictJoin: request.StrictJoin
        );

        await db.Workspaces.AddAsync(workspace, ct);
        await db.SaveChangesAsync(ct);
        
        // 1. Instant Trigger for Background Seeding
        backgroundJob.TriggerOutbox();

        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);

        // 2. STAGE 1 Notification: Workspace record exists, but seeding is starting in background
        await realtime.NotifyUserAsync(currentUserId, "WorkspaceCreating", new { WorkspaceId = workspace.Id }, ct);

        return Result<Guid>.Success(workspace.Id);
    }
}
