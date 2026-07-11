using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateWorkspaceStatusesHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateWorkspaceStatusesHandler> logger
) : ICommandHandler<UpdateWorkspaceStatusesCommand, long>
{
    public async Task<Result<long>> Handle(UpdateWorkspaceStatusesCommand request, CancellationToken cancellationToken)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;

        syncPermission.RequireAdmin();

        var workspaceId = workspaceContext.WorkspaceId;
        List<SyncEventPayload>? broadcastPayloads = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<long>.Success(0);
            }

            var existingStatuses = await db.Statuses
                .Where(s => s.ProjectWorkspaceId == workspaceId && s.DeletedAt == null)
                .ToDictionaryAsync(s => s.Id, cancellationToken);

            var syncEvents = new List<SyncEvent>();

            foreach (var dto in request.Statuses)
            {
                Status? status = null;
                SyncAction action;

                switch (dto.Action)
                {
                    case RowAction.Create:
                        status = Status.Create(workspaceId, dto.SpaceId, dto.Name, dto.Color, memberId, dto.OrderKey, dto.Id);
                        db.Statuses.Add(status);
                        action = SyncAction.C;
                        break;

                    case RowAction.Update:
                        if (dto.Id is null || !existingStatuses.TryGetValue(dto.Id.Value, out status)) continue;
                        status.Update(dto.Name, dto.Color, dto.OrderKey);
                        action = SyncAction.U;
                        break;

                    case RowAction.Delete:
                        if (dto.Id is null || !existingStatuses.TryGetValue(dto.Id.Value, out status)) continue;
                        status.SoftDelete();
                        action = SyncAction.D;
                        break;

                    default:
                        continue;
                }

                var syncPayload = action == SyncAction.D
                    ? JsonSerializer.Serialize(new { id = status.Id }, SyncJson.Options)
                    : JsonSerializer.Serialize(new
                    {
                        id = status.Id,
                        spaceId = status.ProjectSpaceId,
                        name = status.Name,
                        color = status.Color,
                        orderKey = status.OrderKey
                    }, SyncJson.Options);

                syncEvents.Add(new SyncEvent
                {
                    ProjectWorkspaceId = workspaceId,
                    EntityType = SyncEntityType.Status,
                    EntityId = status.Id,
                    Action = action,
                    Payload = syncPayload,
                    ClientTraceId = request.TraceId,
                    AuthorUserId = memberId
                });
            }

            db.SyncEvents.AddRange(syncEvents);
            idempotencyService.MarkAsProcessed(request.TraceId);

            broadcastPayloads = syncEvents.Select(SyncQueryService.MapToPayload).ToList();

            logger.LogInformation("Successfully updated {Count} statuses for Workspace {WorkspaceId} with SyncEvents", syncEvents.Count, workspaceId);
            return Result<long>.Success(syncEvents.Count > 0 ? syncEvents[^1].Id : 0);
        }, cancellationToken);

        if (result.IsSuccess && broadcastPayloads is { Count: > 0 })
        {
            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceId, broadcastPayloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for statuses on Workspace {WorkspaceId}", workspaceId),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        return result;
    }
}
