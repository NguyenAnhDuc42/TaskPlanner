using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class Attachment : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string FileUrl { get; private set; } = null!;
    public string? FileType { get; private set; }
    public long FileSize { get; private set; }
    public Guid UploadedById { get; private set; }

    private Attachment() { } // EF Core

    private Attachment(Guid id, string fileName, string fileUrl, string? fileType, long fileSize, Guid uploadedById, Guid projectTaskId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileUrl)) throw new ArgumentException("File URL cannot be empty.", nameof(fileUrl));
        if (uploadedById == Guid.Empty) throw new ArgumentException("UploadedById cannot be empty.", nameof(uploadedById));
        if (projectTaskId == Guid.Empty) throw new ArgumentException("ProjectTaskId cannot be empty.", nameof(projectTaskId));
        if (fileSize < 0) throw new ArgumentOutOfRangeException(nameof(fileSize), "FileSize cannot be negative.");

        FileName = fileName.Trim();
        FileUrl = fileUrl.Trim();
        FileType = string.IsNullOrWhiteSpace(fileType) ? null : fileType.Trim();
        FileSize = fileSize;
        UploadedById = uploadedById;
        ProjectTaskId = projectTaskId;
    }

    public static Attachment Create(string fileName, string fileUrl, string? fileType, Guid uploadedById, Guid projectTaskId, long fileSize = 0)
    {
        return new Attachment(Guid.NewGuid(), fileName, fileUrl, fileType, fileSize, uploadedById, projectTaskId);
    }

    public void UpdateFileMetadata(string? fileType, long? fileSize = null)
    {
        FileType = string.IsNullOrWhiteSpace(fileType) ? null : fileType?.Trim();
        if (fileSize.HasValue)
        {
            if (fileSize.Value < 0) throw new ArgumentOutOfRangeException(nameof(fileSize));
            FileSize = fileSize.Value;
        }
        UpdateTimestamp();
    }
}
