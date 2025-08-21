using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class Comment : Entity
{
    public string Content { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public Guid ProjectTaskId { get; private set; }
    public Guid? ParentCommentId { get; private set; }

    private Comment() { } // For EF Core

    private Comment(Guid id, string content, Guid authorId, Guid projectTaskId, Guid? parentCommentId = null)
    {
        Id = id;
        Content = content;
        AuthorId = authorId;
        ProjectTaskId = projectTaskId;
        ParentCommentId = parentCommentId;
    }

    public static Comment Create(string content, Guid authorId, Guid projectTaskId, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));

        return new Comment(Guid.NewGuid(), content, authorId, projectTaskId, parentCommentId);
    }
}