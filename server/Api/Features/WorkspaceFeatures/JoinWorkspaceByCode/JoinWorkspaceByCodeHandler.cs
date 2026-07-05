using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class JoinWorkspaceByCodeHandler(
    TaskPlanDbContext db,
    CurrentUserService currentUserService,
    RealtimeService realtime,
    ILogger<JoinWorkspaceByCodeHandler> logger
) : ICommandHandler<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>
{
    public async Task<Result<JoinWorkspaceByCodeResult>> Handle(JoinWorkspaceByCodeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
            return Result<JoinWorkspaceByCodeResult>.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var normalizedCode = request.JoinCode.Trim().ToLowerInvariant();
        var workspace = await db.ProjectWorkspaces
            .FirstOrDefaultAsync(w => w.JoinCode.ToLower() == normalizedCode && w.DeletedAt == null, cancellationToken);

        if (workspace == null) return Result<JoinWorkspaceByCodeResult>.Failure(Error.Validation("Workspace.InvalidJoinCode", "Invalid join code."));
        if (workspace.IsArchived) return Result<JoinWorkspaceByCodeResult>.Failure(Error.Validation("Workspace.Archived", "Cannot join an archived workspace."));

        // Legacy bug fixed here: the original query omitted ProjectWorkspaceId, so it could match
        // the caller's membership row in a completely different workspace and reactivate/rejoin
        // that one instead of the workspace being joined by this code.
        var existingMember = await db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.UserId == currentUserId && m.ProjectWorkspaceId == workspace.Id, cancellationToken);

        JoinWorkspaceByCodeResult dataResult;
        SyncAction memberSyncAction;
        if (existingMember is null)
        {
            workspace.AddMemberByCode(currentUserId, currentUserId);
            var status = workspace.StrictJoin ? MembershipStatus.Pending : MembershipStatus.Active;
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, status.ToString(), true);
            memberSyncAction = SyncAction.C;
        }
        else if (existingMember.DeletedAt != null)
        {
            existingMember.RestoreForJoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
            memberSyncAction = SyncAction.C; // was soft-deleted — other clients no longer have this row at all
        }
        else
        {
            existingMember.JoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
            memberSyncAction = SyncAction.U;
        }

        var member = existingMember ?? workspace.Members.First(m => m.UserId == currentUserId);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == currentUserId, cancellationToken);

        var syncEvent = new SyncEvent
        {
            ProjectWorkspaceId = workspace.Id,
            EntityType = SyncEntityType.Member,
            EntityId = member.Id,
            Action = memberSyncAction,
            Payload = JsonSerializer.Serialize(new
            {
                id = member.Id,
                userId = member.UserId,
                name = user.Name,
                email = user.Email,
                avatarUrl = (string?)null,
                role = member.Role,
                status = member.Status,
                joinedAt = member.JoinedAt
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            ClientTraceId = Guid.NewGuid().ToString(),
            AuthorUserId = currentUserId
        };
        db.SyncEvents.Add(syncEvent);

        await db.SaveChangesAsync(cancellationToken);

        var payload = SyncQueryService.MapToPayload(syncEvent);
        _ = realtime
            .NotifySyncEventAsync(workspace.Id, payload, cancellationToken)
            .ContinueWith(t => logger.LogError(t.Exception, "Failed to send real-time Delta for member join in workspace {WorkspaceId}", workspace.Id), TaskContinuationOptions.OnlyOnFaulted);
        _ = realtime.NotifyUserAsync(currentUserId, "WorkspaceJoined", new { WorkspaceId = workspace.Id }, cancellationToken);

        return Result<JoinWorkspaceByCodeResult>.Success(dataResult);
    }
}
