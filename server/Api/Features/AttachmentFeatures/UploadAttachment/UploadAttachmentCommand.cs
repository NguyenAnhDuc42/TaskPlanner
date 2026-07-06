namespace Api;

public record UploadAttachmentCommand(
    byte[] Content,
    string FileName,
    string ContentType
) : ICommandRequest<UploadAttachmentResult>, IAuthorizedWorkspaceRequest;

public record UploadAttachmentResult(Guid AttachmentId, string Url);
