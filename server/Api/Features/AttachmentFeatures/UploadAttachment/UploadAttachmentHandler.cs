using System.Security.Cryptography;

namespace Api;

public class UploadAttachmentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    ObjectStorageService storage,
    ILogger<UploadAttachmentHandler> logger
) : ICommandHandler<UploadAttachmentCommand, UploadAttachmentResult>
{
    public async Task<Result<UploadAttachmentResult>> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        syncPermission.RequireMember();

        var memberId = workspaceContext.CurrentMember!.Id;
        var workspaceId = workspaceContext.WorkspaceId;

        var checksum = Convert.ToHexString(SHA256.HashData(request.Content));
        var isMedia = request.ContentType.StartsWith("image/") || request.ContentType.StartsWith("video/");

        var attachment = isMedia
            ? Attachment.CreateMedia(workspaceId, request.FileName, request.ContentType, request.Content.Length, checksum, memberId)
            : Attachment.CreateFile(workspaceId, request.FileName, request.ContentType, request.Content.Length, checksum, memberId);

        var key = $"{workspaceId}/{attachment.Id}/{request.FileName}";

        string url;
        try
        {
            using var stream = new MemoryStream(request.Content);
            url = await storage.UploadAsync(stream, key, request.ContentType, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload attachment {FileName} for workspace {WorkspaceId}", request.FileName, workspaceId);
            return Result<UploadAttachmentResult>.Failure(AttachmentError.UploadFailed);
        }

        attachment.MarkReady(key, url, StorageProvider.S3);

        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Uploaded attachment {AttachmentId} ({FileName}) for workspace {WorkspaceId}", attachment.Id, request.FileName, workspaceId);

        return Result<UploadAttachmentResult>.Success(new UploadAttachmentResult(attachment.Id, url));
    }
}
