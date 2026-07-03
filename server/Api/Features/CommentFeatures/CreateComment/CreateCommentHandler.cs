using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class CreateCommentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    RealtimeService realtimeService,
    NotificationService notificationService,
    IdempotencyService idempotencyService,
    ILogger<CreateCommentHandler> logger
) : ICommandHandler<CreateCommentCommand, long>
{
    public async Task<Result<long>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create comment on Task {TaskId}", request.ProjectTaskId);

        var task = await db.ProjectTasks.AsNoTracking()
            .Where(t => t.Id == request.ProjectTaskId && t.DeletedAt == null)
            .Select(t => new { t.ProjectSpaceId, t.ProjectWorkspaceId, t.CreatorId, t.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found or deleted", request.ProjectTaskId);
            return Result<long>.Failure(TaskError.NotFound);
        }

        syncPermission.RequireMember();

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

            var comment = Comment.Create(request.Id, request.Content, memberId, request.ProjectTaskId, request.ParentCommentId);
            db.Comments.Add(comment);

            var syncPayload = JsonSerializer.Serialize(new
            {
                id = comment.Id,
                taskId = comment.ProjectTaskId,
                content = comment.Content,
                isEdited = comment.IsEdited,
                parentCommentId = comment.ParentCommentId,
                creatorId = comment.CreatorId,
                createdAt = comment.CreatedAt
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            syncEvent = new SyncEvent
            {
                ProjectWorkspaceId = task.ProjectWorkspaceId,
                EntityType = SyncEntityType.Comment,
                EntityId = comment.Id,
                Action = SyncAction.C,
                Payload = syncPayload,
                ClientTraceId = request.TraceId,
                AuthorUserId = memberId
            };

            db.SyncEvents.Add(syncEvent);

            idempotencyService.MarkAsProcessed(request.TraceId);

            logger.LogInformation("Successfully created comment {CommentId} in database with SyncEvent", comment.Id);
            return Result<long>.Success(0);
        }, cancellationToken);

        if (result.IsSuccess && syncEvent != null)
        {
            var payload = SyncQueryService.MapToPayload(syncEvent);

            _ = realtimeService
                .NotifySyncEventAsync(task.ProjectWorkspaceId, payload, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time Delta for comment {CommentId}", request.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            // Notify all assignees + task creator (excluding the commenter), plus anyone
            // @mentioned — mirrors the legacy AddCommentHandler's behavior, which this
            // Sync-engine handler otherwise fully replaces.
            var actorUserId = workspaceContext.CurrentMember?.UserId ?? Guid.Empty;
            var recipientUserIds = await db.TaskAssignments
                .Where(a => a.ProjectTaskId == request.ProjectTaskId && a.DeletedAt == null)
                .Select(a => a.WorkspaceMemberId)
                .Join(db.WorkspaceMembers.Where(m => m.DeletedAt == null), id => id, m => m.Id, (_, m) => m.UserId)
                .ToListAsync(cancellationToken);

            if (task.CreatorId.HasValue)
                recipientUserIds.Add(task.CreatorId.Value);

            var actorName = await db.Users.AsNoTracking()
                .Where(u => u.Id == actorUserId)
                .Select(u => u.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "Someone";

            // Detect @[workspaceMemberId] tokens — ID-stable, no name matching needed
            var mentionedMemberIds = MentionRegex.Matches(request.Content)
                .Select(m => m.Groups[1].Value)
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

            var mentionedUserIds = new HashSet<Guid>();
            if (mentionedMemberIds.Count > 0)
            {
                var matched = await db.WorkspaceMembers
                    .Where(m => mentionedMemberIds.Contains(m.Id) && m.DeletedAt == null)
                    .Select(m => m.UserId)
                    .ToListAsync(cancellationToken);
                mentionedUserIds = matched.ToHashSet();
                recipientUserIds.AddRange(mentionedUserIds);
            }

            var snippet = request.Content.Length > 100 ? request.Content[..100] + "…" : request.Content;
            foreach (var recipientId in recipientUserIds.Distinct().Where(id => id != actorUserId))
            {
                var isMention = mentionedUserIds.Contains(recipientId);
                _ = notificationService.PushAsync(
                    recipientId, actorUserId, task.ProjectWorkspaceId,
                    isMention ? "mention" : "comment_added",
                    "task", request.ProjectTaskId,
                    isMention
                        ? $"{actorName} mentioned you in \"{task.Name}\""
                        : $"{actorName} commented on \"{task.Name}\"",
                    snippet,
                    cancellationToken);
            }

            return Result<long>.Success(syncEvent.Id);
        }

        return Result<long>.Failure(result.Error ?? Error.Failure("Transaction.Failed", "Unknown transaction failure"));
    }

    private static readonly System.Text.RegularExpressions.Regex MentionRegex =
        new(@"@\[([a-f0-9\-]{36})\]", System.Text.RegularExpressions.RegexOptions.Compiled);
}
