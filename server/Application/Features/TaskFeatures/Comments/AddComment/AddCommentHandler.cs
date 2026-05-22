using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Helpers;
using Domain.Entities;
using Dapper;

namespace Application.Features.TaskFeatures;

public class AddCommentHandler(IDataBase db, WorkspaceContext workspaceContext) : ICommandHandler<AddCommentCommand, CommentDto>
{
    public async Task<Result<CommentDto>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        const string sql = "SELECT id FROM project_tasks WHERE id = @TaskId AND project_workspace_id = @WorkspaceId AND deleted_at IS NULL;";
        var taskId = await db.Connection.QuerySingleOrDefaultAsync<Guid?>(sql, new { request.TaskId, WorkspaceId = workspaceContext.workspaceId });
        
        if (taskId == null)
            return Result<CommentDto>.Failure(Error.NotFound("Task.NotFound", "Task not found."));

        var comment = Comment.Create(request.Content, workspaceContext.userId, request.TaskId, request.ParentCommentId);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        var dto = new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatorId = comment.CreatedById,
            ProjectTaskId = comment.ProjectTaskId,
            ParentCommentId = comment.ParentCommentId,
            IsEdited = comment.IsEdited,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };

        return Result<CommentDto>.Success(dto);
    }
}
