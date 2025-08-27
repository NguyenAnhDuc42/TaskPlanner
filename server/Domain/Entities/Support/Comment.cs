using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class Comment : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public string Content { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public DateTime PostedAt { get; private set; }
    public bool IsEdited { get; private set; }
    public Guid? ParentCommentId { get; private set; }

    private Comment() { } // EF Core

    private Comment(Guid id, string content, Guid authorId, Guid projectTaskId, Guid? parentCommentId = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        if (authorId == Guid.Empty) throw new ArgumentException("AuthorId cannot be empty.", nameof(authorId));
        if (projectTaskId == Guid.Empty) throw new ArgumentException("ProjectTaskId cannot be empty.", nameof(projectTaskId));

        Content = content.Trim();
        AuthorId = authorId;
        ProjectTaskId = projectTaskId;
        ParentCommentId = parentCommentId;
        PostedAt = CreatedAt;
        IsEdited = false;
    }

    public static Comment Create(string content, Guid authorId, Guid projectTaskId, Guid? parentCommentId = null)
        => new Comment(Guid.NewGuid(), content, authorId, projectTaskId, parentCommentId);

    public void UpdateContent(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent)) throw new ArgumentException("Comment content cannot be empty.", nameof(newContent));
        if (Content == newContent.Trim()) return;

        Content = newContent.Trim();
        IsEdited = true;
        UpdateTimestamp();
    }
}
