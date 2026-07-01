using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

file static class DeleteSpaceSerializerOptions
{
    internal static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}

public class DeleteSpaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<DeleteSpaceHandler> logger
) : ICommandHandler<DeleteSpaceCommand, long>
{
    public async Task<Result<long>> Handle(DeleteSpaceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete space {SpaceId}", request.SpaceId);

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        var spaceData = await db.ProjectSpaces
            .Where(s => s.Id == request.SpaceId && s.DeletedAt == null)
            .Select(s => new {
                Space = s,
                CallerAccess = db.EntityAccesses
                    .Where(ea => ea.ProjectSpaceId == s.Id && ea.WorkspaceMemberId == memberId && ea.DeletedAt == null)
                    .Select(ea => (AccessLevel?)ea.AccessLevel).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var space = spaceData?.Space;
        if (space == null)
        {
            logger.LogWarning("Space {SpaceId} not found or already deleted", request.SpaceId);
            return Result<long>.Failure(SpaceError.NotFound);
        }
        if (space.ProjectWorkspaceId != workspaceContext.WorkspaceId) return Result<long>.Failure(SpaceError.NotFound);

        if (!permissionService.Verify(Role.Admin, space.IsPrivate, spaceData!.CallerAccess, AccessLevel.Manager, space.CreatorId))
        {
            logger.LogWarning("Access denied for user to delete space {SpaceId}", space.Id);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

        SyncEvent? syncEvent = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            // Bulk cascade soft-delete — no individual sync events needed.
            // The client cascades children locally when it receives the Space D event.
            var now = DateTimeOffset.UtcNow;

            await db.ProjectTasks
                .Where(t => t.ProjectSpaceId == space.Id && t.DeletedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.DeletedAt, now)
                    .SetProperty(t => t.UpdatedAt, now), cancellationToken);

            await db.ProjectFolders
                .Where(f => f.ProjectSpaceId == space.Id && f.DeletedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(f => f.DeletedAt, now)
                    .SetProperty(f => f.UpdatedAt, now), cancellationToken);

            space.Delete();

            var syncPayload = JsonSerializer.Serialize(new { id = space.Id }, DeleteSpaceSerializerOptions.CamelCase);

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = space.ProjectWorkspaceId,
                EntityType = SyncEntityType.Space,
                EntityId = space.Id,
                Action = SyncAction.D,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = workspaceContext.CurrentMember?.Id ?? Guid.Empty
            };

            db.SyncEvents.Add(syncEvent);
            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Deleted space {SpaceId}, cascaded folders + tasks via bulk UPDATE", space.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(workspaceContext.WorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for deleted space {SpaceId}", space.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }
}
