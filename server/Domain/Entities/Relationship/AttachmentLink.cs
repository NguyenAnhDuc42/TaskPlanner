using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Relationship;

public class AttachmentLink : Composite
{
    public Guid AttachmentId { get; private set; }
    public Guid ParentEntityId { get; private set; }
    public EntityType ParentEntityType { get; private set; }

    private AttachmentLink() { }
    private AttachmentLink(Guid attachmentId, Guid entityId, EntityType entityType, Guid creatorId)
    {
        AttachmentId = attachmentId;
        ParentEntityId = entityId;
        ParentEntityType = entityType;
        CreatorId = creatorId;
    }
    public static AttachmentLink Link(Guid attachmentId, Guid entityId, EntityType entityType, Guid creatorId) =>
        new AttachmentLink(attachmentId, entityId, entityType, creatorId);
}
