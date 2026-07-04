using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class AddMembersHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    NotificationService notificationService,
    IdempotencyService idempotencyService,
    ILogger<AddMembersHandler> logger
) : ICommandHandler<AddMembersCommand, AddMembersResult>
{
    public async Task<Result<AddMembersResult>> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess)
            return Result<AddMembersResult>.Failure(MemberError.DontHavePermission);

        var callerRole = workspaceContext.CurrentMember!.Role;
        var callerMemberId = workspaceContext.CurrentMember.Id;
        var workspaceId = workspaceContext.WorkspaceId;

        // Cannot assign a role higher than your own
        if (request.Members.Any(m => !callerRole.IsAtLeast(m.Role)))
            return Result<AddMembersResult>.Failure(Error.Forbidden("Member.RoleEscalation", "You cannot assign a role higher than your own."));

        var lowerEmails = request.Members.Select(m => m.Email.ToLower()).ToList();

        // Happy path: match only users that exist — unknown emails are silently skipped
        var users = await db.Users
            .Where(u => lowerEmails.Contains(u.Email.ToLower()) && u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return Result<AddMembersResult>.Failure(Error.NotFound("User.NoneFound", "None of the provided email addresses belong to registered users."));

        var existingUserIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == workspaceId && wm.DeletedAt == null)
            .Select(wm => wm.UserId)
            .ToHashSetAsync(cancellationToken);

        var memberRoleByEmail = request.Members.ToDictionary(m => m.Email, m => m.Role, StringComparer.OrdinalIgnoreCase);

        // Skip users that are already members
        var newMembers = users
            .Where(u => !existingUserIds.Contains(u.Id))
            .Select(u => WorkspaceMember.Create(u.Id, workspaceId, memberRoleByEmail[u.Email], MembershipStatus.Active, callerMemberId, "Invite"))
            .ToList();

        if (newMembers.Count == 0)
            return Result<AddMembersResult>.Failure(Error.Conflict("Member.AlreadyExists", "All specified users are already members of this workspace."));

        var userLookup = users.ToDictionary(u => u.Id);
        List<SyncEventPayload>? broadcastPayloads = null;

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                logger.LogInformation("Idempotent bypass for trace {TraceId}. Skipping.", request.TraceId);
                return Result<AddMembersResult>.Success(new AddMembersResult(0, []));
            }

            db.WorkspaceMembers.AddRange(newMembers);

            var syncEvents = new List<SyncEvent>();
            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            foreach (var member in newMembers)
            {
                var user = userLookup[member.UserId];
                var syncPayload = JsonSerializer.Serialize(new
                {
                    id = member.Id,
                    userId = member.UserId,
                    name = user.Name,
                    email = user.Email,
                    avatarUrl = (string?)null, // User entity has no AvatarUrl field — matches legacy MemberRecord.FromDomain
                    role = member.Role,
                    status = member.Status,
                    joinedAt = member.JoinedAt
                }, jsonOptions);

                syncEvents.Add(new SyncEvent
                {
                    ProjectWorkspaceId = workspaceId,
                    EntityType = SyncEntityType.Member,
                    EntityId = member.Id,
                    Action = SyncAction.C,
                    Payload = syncPayload,
                    ClientTraceId = request.TraceId,
                    AuthorUserId = callerMemberId
                });
            }

            db.SyncEvents.AddRange(syncEvents);
            idempotencyService.MarkAsProcessed(request.TraceId);

            broadcastPayloads = syncEvents.Select(SyncQueryService.MapToPayload).ToList();

            var records = newMembers.Select(m => MemberRecord.FromDomain(m, userLookup[m.UserId])).ToList();

            logger.LogInformation("Successfully added {Count} members to workspace {WorkspaceId} with SyncEvents", syncEvents.Count, workspaceId);
            return Result<AddMembersResult>.Success(new AddMembersResult(syncEvents.Count > 0 ? syncEvents[^1].Id : 0, records));
        }, cancellationToken);

        if (result.IsSuccess && broadcastPayloads is { Count: > 0 })
        {
            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceId, broadcastPayloads, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time DeltaBatch for member adds in workspace {WorkspaceId}", workspaceId),
                    TaskContinuationOptions.OnlyOnFaulted);

            foreach (var member in newMembers)
            {
                var user = userLookup[member.UserId];
                _ = notificationService.PushAsync(
                    recipientUserId: member.UserId,
                    actorUserId: workspaceContext.CurrentMember.UserId,
                    projectWorkspaceId: workspaceId,
                    type: "WorkspaceInvite",
                    entityType: "Workspace",
                    entityId: workspaceId,
                    title: "You've been added to a workspace",
                    cancellationToken: cancellationToken);

                _ = realtimeService.NotifyUserAsync(member.UserId, "WorkspaceJoined", new { WorkspaceId = workspaceId }, cancellationToken);
            }
        }

        return result;
    }
}
