using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Attachment : TenantEntity
{
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

    private Attachment(Guid id, Guid projectWorkspaceId, string fileName, string contentType, long sizeBytes, string checksum, Guid creatorId, bool isPublic)
        : base(id, projectWorkspaceId)
    {
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Checksum = checksum;
        IsPublic = isPublic;
        Type = AttachmentType.File; // Default for this constructor
        ProcessingState = AttachmentProcessingState.Processing;

        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    // --- Explicit Factory Methods ---

    public static Attachment CreateFile(Guid projectWorkspaceId, string fileName, string contentType, long sizeBytes, string checksum, Guid creatorId, bool isPublic = false)
    {
        return new Attachment(Guid.NewGuid(), projectWorkspaceId, fileName, contentType, sizeBytes, checksum, creatorId, isPublic);
    }


    public static Attachment CreateMedia(Guid projectWorkspaceId, string fileName, string contentType, long sizeBytes, string checksum, Guid creatorId, bool isPublic = false)
    {
        var attachment = new Attachment(Guid.NewGuid(), projectWorkspaceId, fileName, contentType, sizeBytes, checksum, creatorId, isPublic);
        attachment.Type = AttachmentType.Media;
        attachment.ProcessingState = AttachmentProcessingState.Uploading;
        attachment.Metadata = new MediaMetaData(null, null, null);
        return attachment;
    }

    public static Attachment CreateLink(Guid projectWorkspaceId, string url, string title, string description, string? imageUrl, Guid creatorId, bool isPublic = false)
    {
        var attachment = new Attachment(Guid.NewGuid(), projectWorkspaceId, string.IsNullOrWhiteSpace(title) ? url : title, "text/uri-list", 0, "", creatorId, isPublic);
        attachment.Type = AttachmentType.Link;
        attachment.StoragePath = url;
        attachment.ProcessingState = AttachmentProcessingState.Ready;
        attachment.Metadata = new LinkMetadata(url, title, description, imageUrl);
        return attachment;
    }

    public static Attachment CreateEmbed(Guid projectWorkspaceId, string embedUrl, string provider, string title, Guid creatorId, bool isPublic = false)
    {
        var attachment = new Attachment(Guid.NewGuid(), projectWorkspaceId, string.IsNullOrWhiteSpace(title) ? provider : title, "text/html", 0, "", creatorId, isPublic);
        attachment.Type = AttachmentType.Embed;
        attachment.StoragePath = embedUrl;
        attachment.ProcessingState = AttachmentProcessingState.Ready;
        attachment.Metadata = new EmbedMetadata(embedUrl, provider);
        return attachment;
    }


    // --- State Transitions ---

    public void MarkReady(string storageKey, string storagePath, StorageProvider provider, AttachmentMetadata? finalMetadata = null)
    {
        if (ProcessingState != AttachmentProcessingState.Uploading && ProcessingState != AttachmentProcessingState.Processing)
            throw new InvalidOperationException("Invalid processing state for transition to Ready.");

        StorageKey = storageKey;
        StoragePath = storagePath;
        StorageProvider = provider;
        ProcessingState = AttachmentProcessingState.Ready;

        if (finalMetadata != null) Metadata = finalMetadata;
        UpdateTimestamp();
    }

    public void MarkFailed()
    {
        ProcessingState = AttachmentProcessingState.Failed;
        UpdateTimestamp();
    }
}