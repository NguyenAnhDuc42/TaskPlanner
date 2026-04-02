using Domain.Common;
using Domain.Enums;

public class AttachmentLink : Entity
{
    public Guid AttachmentId { get; private set; }
    public Guid ParentEntityId { get; private set; }
    public EntityType ParentEntityType { get; private set; }

    private AttachmentLink() { }

    public static AttachmentLink Create(Guid attachmentId, Guid entityId, EntityType entityType, Guid creatorId)
    {
        return new AttachmentLink
        {
            AttachmentId = attachmentId,
            ParentEntityId = entityId,
            ParentEntityType = entityType,
            CreatorId = creatorId
        };
    }
}