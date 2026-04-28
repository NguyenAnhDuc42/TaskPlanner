using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public class CreateWorkspaceHandler(
    IDataBase db, 
    ICurrentUserService currentUserService, 
    HybridCache cache, 
    IRealtimeService realtime,
    WorkspaceService workspaceService
) : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result<Guid>.Failure(UserError.NotFound);

        // 1. Create the Workspace Shell (Fast)
        var workspace = ProjectWorkspace.Create(
            name: request.Name,
            slug: SlugHelper.GenerateSlug(request.Name),
            description: request.Description ?? string.Empty,
            joinCode: null,
            color: request.Color,
            icon: request.Icon,
            creatorId: currentUserId,
            theme: request.Theme,
            strictJoin: request.StrictJoin
        );
        
        await db.Workspaces.AddAsync(workspace, ct);
        await db.SaveChangesAsync(ct);

        // 2. Clear Cache
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);

        // 3. Offload Skeleton Creation (Async Internal Service)
        workspaceService.InitializeInBackground(workspace.Id, currentUserId);

        // 4. Immediate Real-time Signal for Navigation
        await realtime.NotifyUserAsync(currentUserId, "WorkspaceCreated", new { WorkspaceId = workspace.Id }, ct);

        return Result<Guid>.Success(workspace.Id);
    }
}
