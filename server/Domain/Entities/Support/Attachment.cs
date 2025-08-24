using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class Attachment : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string FileUrl { get; private set; } = null!;
    public string? FileType { get; private set; }
    public long FileSize { get; private set; }
    public Guid UploadedById { get; private set; }

    private Attachment() { } // For EF Core

    private Attachment(Guid id, string fileName, string fileUrl, string fileType, Guid uploadedById, Guid projectTaskId)
    {
        Id = id;
        FileName = fileName;
        FileUrl = fileUrl;
        FileType = fileType;
        UploadedById = uploadedById; // Assign to the existing UploadedById property
        ProjectTaskId = projectTaskId;
        FileSize = 0; // Initialize FileSize
    }

    public static Attachment Create(string fileName, string fileUrl, string fileType, Guid uploadedById, Guid projectTaskId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("File URL cannot be empty.", nameof(fileUrl));

        return new Attachment(Guid.NewGuid(), fileName, fileUrl, fileType, uploadedById, projectTaskId);
    }
}