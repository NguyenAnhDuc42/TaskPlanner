using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class Comment : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public string Content { get; private set; } = null!;
    public bool IsEdited { get; private set; }
    public Guid? ParentCommentId { get; private set; }

    private Comment() { } // EF Core

    private Comment(Guid id, string content, Guid creatorId, Guid projectTaskId, Guid? parentCommentId = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        if (projectTaskId == Guid.Empty) throw new ArgumentException("ProjectTaskId cannot be empty.", nameof(projectTaskId));

        Content = content.Trim();
        CreatorId = creatorId;
        ProjectTaskId = projectTaskId;
        ParentCommentId = parentCommentId;
        IsEdited = false;
    }

    public static Comment Create(string content, Guid creatorId, Guid projectTaskId, Guid? parentCommentId = null)
        => new Comment(Guid.NewGuid(), content, creatorId, projectTaskId, parentCommentId);

    public void UpdateContent(string newContent)
    {
        if (string.IsNullOrWhiteSpace(newContent)) throw new ArgumentException("Comment content cannot be empty.", nameof(newContent));
        if (Content == newContent.Trim()) return;

        Content = newContent.Trim();
        IsEdited = true;
        UpdateTimestamp();
    }
}
