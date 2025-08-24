using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class Comment : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public string Content { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public DateTime PostedAt { get; private set; }
    public bool IsEdited { get; private set; }
    public Guid? ParentCommentId { get; private set; }

    private Comment() { } // For EF Core

    private Comment(Guid id, string content, Guid authorId, Guid projectTaskId, Guid? parentCommentId = null)
    {
        Id = id;
        Content = content;
        AuthorId = authorId;
        ProjectTaskId = projectTaskId;
        ParentCommentId = parentCommentId;
        PostedAt = DateTime.UtcNow; // Initialize PostedAt
        IsEdited = false; // Initialize IsEdited
    }

    public static Comment Create(string content, Guid authorId, Guid projectTaskId, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));

        return new Comment(Guid.NewGuid(), content, authorId, projectTaskId, parentCommentId);
    }

    public void UpdateContent(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Comment content cannot be empty.", nameof(newContent));
        
        if (Content == newContent) return;

        Content = newContent;
        // Note: You might want to add a domain event here, e.g., CommentUpdatedEvent
    }
}