using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateWorkspaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateWorkspaceHandler> logger
) : ICommandHandler<UpdateWorkspaceCommand, long>
{
    public async Task<Result<long>> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await db.ProjectWorkspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.DeletedAt == null, cancellationToken);

        if (workspace is null)
            return Result<long>.Failure(WorkspaceError.NotFound);

        if (workspace.CreatorId != workspaceContext.CurrentMember?.UserId)
        {
            logger.LogWarning("User {UserId} is not the owner of workspace {WorkspaceId}", workspaceContext.CurrentMember?.UserId, workspace.Id);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

            workspace.Update(
                name: request.Name,
                slug: slug,
                description: request.Description,
                color: request.Color,
                icon: request.Icon,
                strictJoin: request.StrictJoin
            );

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = workspace.Id,
                name = workspace.Name,
                description = workspace.Description,
                color = workspace.Color,
                icon = workspace.Icon,
                strictJoin = workspace.StrictJoin
            }, SyncJson.Options);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = workspace.Id,
                EntityType = SyncEntityType.Workspace,
                EntityId = workspace.Id,
                Action = SyncAction.U,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully updated workspace {WorkspaceId} in database with SyncEvent", workspace.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspace.Id, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for workspace {WorkspaceId}", workspace.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
