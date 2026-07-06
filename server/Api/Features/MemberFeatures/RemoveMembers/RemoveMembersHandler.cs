using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class RemoveMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<RemoveMembersHandler> logger
) : ICommandHandler<RemoveMembersCommand, long>
{
    public async Task<Result<long>> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess)
            return Result<long>.Failure(MemberError.DontHavePermission);

        var callerRole = workspaceContext.CurrentMember!.Role;
        var callerMemberId = workspaceContext.CurrentMember.Id;
        var workspaceId = workspaceContext.WorkspaceId;

        // Self-removal must go through "Leave Workspace" instead.
        if (request.MemberIds.Contains(callerMemberId))
        {
            logger.LogWarning("User {UserId} attempted to remove themselves via RemoveMembers", workspaceContext.CurrentMember.UserId);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

        var targets = await db.WorkspaceMembers
            .Where(m => request.MemberIds.Contains(m.Id) && m.ProjectWorkspaceId == workspaceId && m.DeletedAt == null)
            .ToListAsync(cancellationToken);

        // Caller cannot remove a peer or superior.
        if (targets.Any(t => t.Role >= callerRole))
        {
            logger.LogWarning("User {UserId} attempted to remove a peer/superior member", workspaceContext.CurrentMember.UserId);
            return Result<long>.Failure(MemberError.DontHavePermission);
        }

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

            foreach (var target in targets)
            {
                target.SoftDelete();

                var syncPayload = JsonSerializer.Serialize(new { id = target.Id },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                syncEvents.Add(new SyncEvent
                {
                    ProjectWorkspaceId = workspaceId,
                    EntityType = SyncEntityType.Member,
                    EntityId = target.Id,
                    Action = SyncAction.D,
                    Payload = syncPayload,
                    ClientTraceId = request.TraceId,
                    AuthorUserId = callerMemberId
                });
            }

            db.SyncEvents.AddRange(syncEvents);
            idempotencyService.MarkAsProcessed(request.TraceId);

            broadcastPayloads = syncEvents.Select(SyncQueryService.MapToPayload).ToList();

            logger.LogInformation("Successfully removed {Count} members from workspace {WorkspaceId} with SyncEvents", syncEvents.Count, workspaceId);
            return Result<long>.Success(syncEvents.Count > 0 ? syncEvents[^1].Id : 0);
        }, cancellationToken);

        if (result.IsSuccess && broadcastPayloads is { Count: > 0 })
        {
            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceId, broadcastPayloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for member removals in workspace {WorkspaceId}", workspaceId),
                    TaskContinuationOptions.OnlyOnFaulted);

            foreach (var target in targets)
            {
                _ = realtimeService.NotifyUserAsync(target.UserId, "WorkspaceAccessRevoked", new { WorkspaceId = workspaceId }, cancellationToken);
            }
        }

        return result;
    }
}
