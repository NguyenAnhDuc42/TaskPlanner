using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Support;

public class Attachment : Entity
{
    public string ContentId { get; private set; } = "";        // "sha256:..." or object key
    public StorageProvider StorageProvider { get; private set; }   // e.g., "S3", "GCS", "Local" - minimal string to avoid enum coupling
    public string StoragePath { get; private set; } = "";        // bucket/key or logical path (not required for domain but convenient)

    // Visible metadata
    public string FileName { get; private set; } = "";
    public string ContentType { get; private set; } = ""; // mime type
    public long SizeBytes { get; private set; }
    public string Checksum { get; private set; } = "";
    public string ChecksumAlgorithm { get; private set; } = "SHA256";

    // Lifecycle & state
    public AttachmentProcessingState ProcessingState { get; private set; }
    public bool IsPublic { get; private set; }
    // Operational
    public int LinkCount { get; private set; } = 0;       // gc/ref-count hint (keeps things simple)
    public string CustomMetaJson { get; private set; } = "{}"; // exif, dims, thumbnails pointers
    protected Attachment() { }

    public static Attachment Create(
        string contentId, StorageProvider storageProvider, string storagePath,
        string fileName, string contentType, long sizeBytes, string checksum,bool isPublic = false, string? customMetaJson = null,Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(contentId)) throw new ArgumentException(nameof(contentId));
        if (sizeBytes < 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));

        return new Attachment
        {
            Id = Guid.NewGuid(),
            ContentId = contentId,
            StorageProvider = storageProvider,
            StoragePath = storagePath,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Checksum = checksum,
            ProcessingState = AttachmentProcessingState.Uploaded,
            IsPublic = isPublic,
            CustomMetaJson = customMetaJson ?? "{}",
            LinkCount = 0,
            CreatorId = creatorId
        };
    }

    // Behavior
    public void MarkScanning() => ProcessingState = AttachmentProcessingState.Scanning;

    public void MarkReady()
    {
        ProcessingState = AttachmentProcessingState.Ready;
        // DomainEvents.Raise(new AttachmentReadyEvent(Id, ContentId, SizeBytes));
    }

    public void IncrementLinkCount()
    {
        LinkCount++;
    }

    public void DecrementLinkCount()
    {
        LinkCount = Math.Max(0, LinkCount - 1);
    }

    public void SetCustomMeta(string json) => CustomMetaJson = json ?? "{}";

    public void SetPublic(bool isPublic) => IsPublic = isPublic;
}
