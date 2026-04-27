using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class AttachmentLink : Entity
{
    public Guid AttachmentId { get; private set; }
    public Guid? ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public Guid? ProjectTaskId { get; private set; }
    public Guid? CommentId { get; private set; }

    private AttachmentLink() { }

    private AttachmentLink(Guid attachmentId, Guid? projectSpaceId, Guid? projectFolderId, Guid? projectTaskId, Guid? commentId, Guid creatorId)
        : base(Guid.NewGuid())
    {
        AttachmentId = attachmentId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        ProjectTaskId = projectTaskId;
        CommentId = commentId;
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static AttachmentLink Create(Guid attachmentId, Guid? projectSpaceId, Guid? projectFolderId, Guid? projectTaskId, Guid? commentId, Guid creatorId)
    {
        return new AttachmentLink(attachmentId, projectSpaceId, projectFolderId, projectTaskId, commentId, creatorId);
    }
}