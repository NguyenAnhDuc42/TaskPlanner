using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class DeleteWorkspaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    RealtimeService realtimeService,
    ILogger<DeleteWorkspaceHandler> logger
) : ICommandHandler<DeleteWorkspaceCommand, long>
{
    public async Task<Result<long>> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await db.ProjectWorkspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.DeletedAt == null, cancellationToken);

        if (workspace is null)
            return Result<long>.Failure(WorkspaceError.NotFound);

        // Only the workspace creator (owner) can delete it — same narrowing as UpdateWorkspace.
        if (workspace.CreatorId != workspaceContext.CurrentMember?.UserId)
        {
            logger.LogWarning("User {UserId} is not the owner of workspace {WorkspaceId}", workspaceContext.CurrentMember?.UserId, workspace.Id);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;

        workspace.Delete();

        var syncPayload = JsonSerializer.Serialize(new { id = workspace.Id },
            SyncJson.Options);

        var syncEvent = new SyncEvent
        {
            ProjectWorkspaceId = workspace.Id,
            EntityType = SyncEntityType.Workspace,
            EntityId = workspace.Id,
            Action = SyncAction.D,
            Payload = syncPayload,
            ClientTraceId = string.Empty,
            AuthorUserId = memberId
        };

        db.SyncEvents.Add(syncEvent);
        await db.SaveChangesAsync(cancellationToken);

        var payload = SyncQueryService.MapToPayload(syncEvent);

        _ = realtimeService
            .NotifySyncEventAsync(workspace.Id, payload, default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time Delta for deleted workspace {WorkspaceId}", workspace.Id),
                TaskContinuationOptions.OnlyOnFaulted);

        logger.LogInformation("Successfully deleted workspace {WorkspaceId}", workspace.Id);
        return Result<long>.Success(syncEvent.Id);
    }
}
