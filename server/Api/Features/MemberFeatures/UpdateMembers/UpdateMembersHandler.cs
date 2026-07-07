using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class UpdateMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    IdempotencyService idempotencyService,
    ILogger<UpdateMembersHandler> logger
) : ICommandHandler<UpdateMembersCommand, long>
{
    public async Task<Result<long>> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess)
            return Result<long>.Failure(MemberError.DontHavePermission);

        var callerRole = workspaceContext.CurrentMember!.Role;
        var memberId = workspaceContext.CurrentMember.Id;
        var workspaceId = workspaceContext.WorkspaceId;

        var ids = request.Members.Select(m => m.MemberId).ToList();
        var targets = await db.WorkspaceMembers
            .Include(m => m.User)
            .Where(m => ids.Contains(m.Id) && m.ProjectWorkspaceId == workspaceId && m.DeletedAt == null)
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        // Caller cannot modify a peer or superior, nor escalate anyone above their own role.
        foreach (var update in request.Members)
        {
            if (!targets.TryGetValue(update.MemberId, out var target)) continue;

            if (!callerRole.IsAtLeast(target.Role) || target.Role >= callerRole)
            {
                logger.LogWarning("User {UserId} cannot modify peer/superior member {MemberId}", workspaceContext.CurrentMember.UserId, update.MemberId);
                return Result<long>.Failure(MemberError.DontHavePermission);
            }

            if (update.Role.HasValue && !callerRole.IsAtLeast(update.Role.Value))
            {
                logger.LogWarning("User {UserId} attempted role escalation to {Role}", workspaceContext.CurrentMember.UserId, update.Role);
                return Result<long>.Failure(MemberError.DontHavePermission);
            }
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

            foreach (var update in request.Members)
            {
                if (!targets.TryGetValue(update.MemberId, out var target)) continue;

                if (update.Status == MembershipStatus.Active && target.Status != MembershipStatus.Active)
                    target.ApproveMembership();
                else
                    target.Update(update.Role, update.Status);

                var syncPayload = JsonSerializer.Serialize(new
                {
                    id = target.Id,
                    userId = target.UserId,
                    name = target.User.Name,
                    email = target.User.Email,
                    avatarUrl = (string?)null, // User entity has no AvatarUrl field — matches legacy MemberRecord.FromDomain
                    role = target.Role,
                    status = target.Status,
                    joinedAt = target.JoinedAt
                }, SyncJson.Options);

                syncEvents.Add(new SyncEvent
                {
                    ProjectWorkspaceId = workspaceId,
                    EntityType = SyncEntityType.Member,
                    EntityId = target.Id,
                    Action = SyncAction.U,
                    Payload = syncPayload,
                    ClientTraceId = request.TraceId,
                    AuthorUserId = memberId
                });
            }

            db.SyncEvents.AddRange(syncEvents);
            idempotencyService.MarkAsProcessed(request.TraceId);

            broadcastPayloads = syncEvents.Select(SyncQueryService.MapToPayload).ToList();

            logger.LogInformation("Successfully updated {Count} members in workspace {WorkspaceId} with SyncEvents", syncEvents.Count, workspaceId);
            return Result<long>.Success(syncEvents.Count > 0 ? syncEvents[^1].Id : 0);
        }, cancellationToken);

        if (result.IsSuccess && broadcastPayloads is { Count: > 0 })
        {
            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceId, broadcastPayloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for member updates in workspace {WorkspaceId}", workspaceId),
                    TaskContinuationOptions.OnlyOnFaulted);

            foreach (var update in request.Members)
            {
                if (!targets.TryGetValue(update.MemberId, out var target)) continue;
                if (target.Status == MembershipStatus.Active) continue;

                _ = realtimeService.NotifyUserAsync(target.UserId, "WorkspaceAccessRevoked", new { WorkspaceId = workspaceId }, cancellationToken);
            }
        }

        return result;
    }
}
