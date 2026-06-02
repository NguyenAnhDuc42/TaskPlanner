using Microsoft.Extensions.Caching.Hybrid;
namespace Application;

public class CreateWorkspaceHandler(
    TaskPlanDbContext db, 
    CurrentUserService currentUserService, 
    HybridCache cache, 
    RealtimeService realtime,
    WorkspaceService workspaceService
) : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
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
        
        await db.ProjectWorkspaces.AddAsync(workspace, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);

        workspaceService.InitializeInBackground(workspace.Id, currentUserId);
        
        await realtime.NotifyUserAsync(currentUserId, "WorkspaceCreated", new { WorkspaceId = workspace.Id }, cancellationToken);

        return Result<Guid>.Success(workspace.Id);
    }
}



