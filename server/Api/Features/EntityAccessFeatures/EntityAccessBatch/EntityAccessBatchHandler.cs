using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class EntityAccessBatchHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<EntityAccessBatchHandler> logger
) : ICommandHandler<EntityAccessBatchCommand, long>
{
    public async Task<Result<long>> Handle(EntityAccessBatchCommand request, CancellationToken cancellationToken)
    {
        var space = await db.ProjectSpaces.AsNoTracking()
            .Where(s => s.Id == request.SpaceId && s.DeletedAt == null)
            .Select(s => new { s.Id, s.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);

        if (space is null)
            return Result<long>.Failure(SpaceError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, space.Id, AccessLevel.Editor, space.CreatorId, cancellationToken);
        if (!hasAccess)
        {
            logger.LogWarning("Access denied for user to update entity access on Space {SpaceId}", request.SpaceId);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

        var deleteRows = request.Rows.Where(r => r.Action == RowAction.Delete).ToList();
        var updateRows = request.Rows.Where(r => r.Action == RowAction.Update).ToList();
        var createRows = request.Rows.Where(r => r.Action == RowAction.Create).ToList();

        var affectedRowIds = deleteRows.Concat(updateRows).Where(r => r.Id.HasValue).Select(r => r.Id!.Value).Distinct().ToList();
        var affectedMemberIds = deleteRows.Concat(updateRows).Where(r => !r.Id.HasValue).Select(r => r.MemberId).Distinct().ToList();

        var existingRows = await db.EntityAccesses
            .Where(ea => ea.ProjectSpaceId == request.SpaceId && ea.DeletedAt == null
                && (affectedRowIds.Contains(ea.Id) || affectedMemberIds.Contains(ea.WorkspaceMemberId)))
            .ToListAsync(cancellationToken);

        var existingById = existingRows.ToDictionary(ea => ea.Id);
        var existingByMember = existingRows.ToDictionary(ea => ea.WorkspaceMemberId);

        // Delete/Update rows must resolve to a real row (by Id first, else by MemberId) — fail fast, no partial application.
        foreach (var row in deleteRows.Concat(updateRows))
        {
            var found = (row.Id.HasValue && existingById.ContainsKey(row.Id.Value)) || existingByMember.ContainsKey(row.MemberId);
            if (!found) return Result<long>.Failure(EntityAccessError.NotFound);
        }

        if (createRows.Count > 0)
        {
            var createMemberIds = createRows.Select(r => r.MemberId).Distinct().ToList();
            var validMemberIds = await db.WorkspaceMembers
                .Where(m => createMemberIds.Contains(m.Id) && m.ProjectWorkspaceId == workspaceContext.WorkspaceId && m.DeletedAt == null)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (validMemberIds.Count != createMemberIds.Count)
                return Result<long>.Failure(EntityAccessError.InvalidMember);
        }

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
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

            var syncEvents = new List<SyncEvent>();

            foreach (var row in deleteRows)
            {
                var entity = (row.Id.HasValue && existingById.TryGetValue(row.Id.Value, out var byId)) ? byId : existingByMember[row.MemberId];
                // Soft-delete (not the legacy hard db.EntityAccesses.Remove) so offline clients
                // get a tombstone D event to reconcile against, matching the rest of the sync system.
                entity.Remove();

                syncEvents.Add(BuildSyncEvent(workspaceId, entity.Id, SyncAction.D, JsonSerializer.Serialize(new { id = entity.Id }, JsonOpts), request.TraceId, memberId));
            }

            foreach (var row in updateRows)
            {
                var entity = (row.Id.HasValue && existingById.TryGetValue(row.Id.Value, out var byId)) ? byId : existingByMember[row.MemberId];
                entity.Update(row.AccessLevel);

                syncEvents.Add(BuildSyncEvent(workspaceId, entity.Id, SyncAction.U, SerializeRow(entity), request.TraceId, memberId));
            }

            foreach (var row in createRows)
            {
                var entity = EntityAccess.Create(workspaceId, row.MemberId, request.SpaceId, null, null, row.AccessLevel, memberId);
                db.EntityAccesses.Add(entity);

                syncEvents.Add(BuildSyncEvent(workspaceId, entity.Id, SyncAction.C, SerializeRow(entity), request.TraceId, memberId));
            }

            db.SyncEvents.AddRange(syncEvents);
            idempotencyService.MarkAsProcessed(request.TraceId);

            broadcastPayloads = syncEvents.Select(SyncQueryService.MapToPayload).ToList();

            logger.LogInformation("Successfully updated {Count} entity access rows for Space {SpaceId} with SyncEvents", syncEvents.Count, request.SpaceId);
            return Result<long>.Success(syncEvents.Count > 0 ? syncEvents[^1].Id : 0);
        }, cancellationToken);

        if (result.IsSuccess && broadcastPayloads is { Count: > 0 })
        {
            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceId, broadcastPayloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for entity access on Space {SpaceId}", request.SpaceId),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        return result;
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static string SerializeRow(EntityAccess entity) => JsonSerializer.Serialize(new
    {
        id = entity.Id,
        spaceId = entity.ProjectSpaceId,
        workspaceMemberId = entity.WorkspaceMemberId,
        accessLevel = entity.AccessLevel,
        haveAccess = true
    }, JsonOpts);

    private static SyncEvent BuildSyncEvent(Guid workspaceId, Guid entityId, SyncAction action, string payload, string traceId, Guid authorUserId) => new()
    {
        ProjectWorkspaceId = workspaceId,
        EntityType = SyncEntityType.EntityAccess,
        EntityId = entityId,
        Action = action,
        Payload = payload,
        ClientTraceId = traceId,
        AuthorUserId = authorUserId
    };
}
