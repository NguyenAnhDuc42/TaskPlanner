using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public partial class AddCommentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    NotificationService notificationService,
    ILogger<AddCommentHandler> logger

) : ICommandHandler<AddCommentCommand, CommentRecord>
{
    public async Task<Result<CommentRecord>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks
            .AsNoTracking()
            .Where(t => t.Id == request.TaskId && t.DeletedAt == null)
            .Select(t => new { t.ProjectSpaceId, t.CreatorId, t.Name, t.ProjectWorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (task == null)
            return Result<CommentRecord>.Failure(Error.NotFound("Task.NotFound", "Task not found."));

        var hasAccess = await permissionService.VerifyAsync(Role.Member, task.ProjectSpaceId, AccessLevel.Viewer, task.CreatorId, cancellationToken);
        if (!hasAccess)
            return Result<CommentRecord>.Failure(MemberError.DontHavePermission);

        var comment = Comment.Create(request.Content, workspaceContext.CurrentMember.UserId, request.TaskId, request.ParentCommentId);
        db.Comments.Add(comment);
        
        var affected = await db.SaveChangesAsync(cancellationToken);

        var dto = new CommentRecord
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatorId = comment.CreatorId ?? Guid.Empty,
            TaskId = comment.ProjectTaskId,
            ParentCommentId = comment.ParentCommentId,
            IsEdited = comment.IsEdited,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };

        if (affected > 0)
        {
            _ = realtimeService
                .NotifyEntitiesUpdatedAsync(
                    workspaceContext.WorkspaceId,
                    new EntityBatchUpdate { Comments = [dto] },
                    default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time notification for added comment {CommentId}", comment.Id),
                    TaskContinuationOptions.OnlyOnFaulted);

            // Notify all assignees + task creator (excluding the commenter)
            var actorUserId = workspaceContext.CurrentMember.UserId;
            var recipientUserIds = await db.TaskAssignments
                .Where(a => a.ProjectTaskId == request.TaskId && a.DeletedAt == null)
                .Select(a => a.WorkspaceMemberId)
                .Join(db.WorkspaceMembers.Where(m => m.DeletedAt == null), id => id, m => m.Id, (_, m) => m.UserId)
                .ToListAsync(cancellationToken);

            if (task.CreatorId.HasValue)
                recipientUserIds.Add(task.CreatorId.Value);

            var actor = await db.Users.AsNoTracking().Where(u => u.Id == actorUserId).Select(u => u.Name).FirstOrDefaultAsync(cancellationToken);
            var actorName = actor ?? "Someone";

            // Detect @mentions — find @Name patterns and notify those members too
            var mentionedNames = MentionRegex.Matches(request.Content)
                .Select(m => m.Groups[1].Value.Trim().ToLowerInvariant())
                .Where(n => n.Length > 0)
                .Distinct()
                .ToList();

            var mentionedUserIds = new HashSet<Guid>();
            if (mentionedNames.Count > 0)
            {
                var matched = await db.WorkspaceMembers
                    .Where(m => m.ProjectWorkspaceId == task.ProjectWorkspaceId && m.DeletedAt == null)
                    .Join(db.Users, m => m.UserId, u => u.Id, (m, u) => new { m.UserId, u.Name })
                    .Where(x => mentionedNames.Contains(x.Name.ToLower()))
                    .Select(x => x.UserId)
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
                    "task", request.TaskId,
                    isMention
                        ? $"{actorName} mentioned you in \"{task.Name}\""
                        : $"{actorName} commented on \"{task.Name}\"",
                    snippet,
                    cancellationToken);
            }
        }

        return Result<CommentRecord>.Success(dto);
    }

    private static readonly System.Text.RegularExpressions.Regex MentionRegex =
        new(@"@([\w]+(?:\s[\w]+)*)(?=\s|$|[^a-zA-Z\s])", System.Text.RegularExpressions.RegexOptions.Compiled);
}
