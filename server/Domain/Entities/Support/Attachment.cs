using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class Attachment : Entity
{
    public string FileName { get; private set; } = null!;
    public string FileUrl { get; private set; } = null!;
    public string FileType { get; private set; } = null!;
    public Guid UploaderId { get; private set; }
    public Guid ProjectTaskId { get; private set; }

    private Attachment() { } // For EF Core

    private Attachment(Guid id, string fileName, string fileUrl, string fileType, Guid uploaderId, Guid projectTaskId)
    {
        Id = id;
        FileName = fileName;
        FileUrl = fileUrl;
        FileType = fileType;
        UploaderId = uploaderId;
        ProjectTaskId = projectTaskId;
    }

    public static Attachment Create(string fileName, string fileUrl, string fileType, Guid uploaderId, Guid projectTaskId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("File URL cannot be empty.", nameof(fileUrl));

        return new Attachment(Guid.NewGuid(), fileName, fileUrl, fileType, uploaderId, projectTaskId);
    }
}