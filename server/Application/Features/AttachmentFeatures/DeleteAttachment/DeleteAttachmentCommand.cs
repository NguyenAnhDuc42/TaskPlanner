namespace Application;

public record DeleteAttachmentCommand(Guid AttachmentId) : ICommandRequest;


