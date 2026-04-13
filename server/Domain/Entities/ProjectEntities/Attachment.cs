using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Attachment : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    
    // Storage Info
    public string StorageKey { get; private set; } = "";
    public StorageProvider StorageProvider { get; private set; }
    public string StoragePath { get; private set; } = "";

    // Metadata
    public string FileName { get; private set; } = "";
    public string ContentType { get; private set; } = "";
    public AttachmentType Type { get; private set; }
    public long SizeBytes { get; private set; }

    // Integrity
    public string Checksum { get; private set; } = "";
    public string ChecksumAlgorithm { get; private set; } = "SHA256";

    // State
    public AttachmentProcessingState ProcessingState { get; private set; }
    public bool IsPublic { get; private set; }

    // Value Object
    public AttachmentMetadata? Metadata { get; private set; }

    protected Attachment() { }

    // --- Explicit Factory Methods ---

    public static Attachment CreateFile(Guid projectWorkspaceId, string fileName, string contentType, long sizeBytes, string checksum, Guid creatorId, bool isPublic = false)
    {
        return new Attachment
        {
            ProjectWorkspaceId = projectWorkspaceId,
            Type = AttachmentType.File,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Checksum = checksum,
            ProcessingState = AttachmentProcessingState.Uploading,
            IsPublic = isPublic,
            CreatorId = creatorId,
            Metadata = new FileMetadata(Path.GetExtension(fileName))
        };
    }

    public static Attachment CreateMedia(Guid projectWorkspaceId, string fileName, string contentType, long sizeBytes, string checksum, Guid creatorId, bool isPublic = false)
    {
        return new Attachment
        {
            ProjectWorkspaceId = projectWorkspaceId,
            Type = AttachmentType.Media,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Checksum = checksum,
            ProcessingState = AttachmentProcessingState.Uploading,
            IsPublic = isPublic,
            CreatorId = creatorId,
            Metadata = new MediaMetaData(null, null, null)
        };
    }

    public static Attachment CreateLink(Guid projectWorkspaceId, string url, string? title, string? description, string? imageUrl, Guid creatorId, bool isPublic = false)
    {
        return new Attachment
        {
            ProjectWorkspaceId = projectWorkspaceId,
            Type = AttachmentType.Link,
            FileName = title ?? url,
            StoragePath = url,
            ContentType = "text/uri-list",
            ProcessingState = AttachmentProcessingState.Ready,
            Metadata = new LinkMetadata(url, title, description, imageUrl),
            IsPublic = isPublic,
            CreatorId = creatorId
        };
    }

    public static Attachment CreateEmbed(Guid projectWorkspaceId, string embedUrl, string provider, string? title, Guid creatorId, bool isPublic = false)
    {
        return new Attachment
        {
            ProjectWorkspaceId = projectWorkspaceId,
            Type = AttachmentType.Embed,
            FileName = title ?? provider,
            StoragePath = embedUrl,
            ContentType = "text/html",
            ProcessingState = AttachmentProcessingState.Ready,
            Metadata = new EmbedMetadata(embedUrl, provider),
            IsPublic = isPublic,
            CreatorId = creatorId
        };
    }


    // --- State Transitions ---

    public void MarkReady(string storageKey, string storagePath, StorageProvider provider, AttachmentMetadata? finalMetadata = null)
    {
        if (ProcessingState != AttachmentProcessingState.Uploading)
            throw new InvalidOperationException("Only uploading attachments can be marked ready.");

        StorageKey = storageKey;
        StoragePath = storagePath;
        StorageProvider = provider;
        ProcessingState = AttachmentProcessingState.Ready;

        if (finalMetadata != null) Metadata = finalMetadata;
    }

    public void MarkFailed()
    {
        ProcessingState = AttachmentProcessingState.Failed;
    }
}