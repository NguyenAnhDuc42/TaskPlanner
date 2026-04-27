using Domain.Common;

namespace Domain.Entities;

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
        Content = content;
        ProjectTaskId = projectTaskId;
        ParentCommentId = parentCommentId;
        IsEdited = false;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static Comment Create(string content, Guid creatorId, Guid projectTaskId, Guid? parentCommentId = null)
        => new Comment(Guid.NewGuid(), content, creatorId, projectTaskId, parentCommentId);

    public void UpdateContent(string newContent)
    {
        Content = newContent;
        IsEdited = true;
        UpdateTimestamp();
    }
}
