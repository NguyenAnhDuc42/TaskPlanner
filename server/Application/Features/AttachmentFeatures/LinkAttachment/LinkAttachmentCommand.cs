namespace Application;

public record LinkAttachmentCommand(
    Guid AttachmentId, 
    Guid ParentEntityId, 
    EntityType ParentEntityType
) : ICommandRequest;


