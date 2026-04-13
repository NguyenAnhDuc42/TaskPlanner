using Application.Helpers;
using Application.Common.Results;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.Support;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.AttachmentFeatures.UploadAttachment;

public class UploadAttachmentHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UploadAttachmentCommand>
{
    public async Task<Result> Handle(UploadAttachmentCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Members or above can upload attachments
        if (context.CurrentMember.Role > Role.Member)
            return Result.Failure(MemberError.DontHavePermission);

        try
        {
            var attachment = request.Type switch
            {
                AttachmentType.File => await CreateFileAttachmentAsync(request),
                AttachmentType.Media => await CreateMediaAttachmentAsync(request),
                AttachmentType.Link => CreateLinkAttachment(request),
                AttachmentType.Embed => CreateEmbedAttachment(request),
                _ => throw new ArgumentException("Invalid Attachment Type")
            };

            // 1. Persist Master Asset (Workspace Pool)
            await db.Attachments.AddAsync(attachment, ct);

            // 2. Create the Relation (The Universal Link)
            var link = EntityAssetLink.Create(
                attachment.Id,
                AssetType.Attachment,
                request.ParentEntityId,
                request.EntityType,
                context.CurrentMember.Id);

            await db.EntityAssetLinks.AddAsync(link, ct);
            await db.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(AttachmentError.UploadFailed);
        }
    }

    private async Task<Attachment> CreateFileAttachmentAsync(UploadAttachmentCommand req)
    {
        if (req.File == null) throw new ArgumentNullException(nameof(req.File));
        var checksum = await CalculateChecksumAsync(req.File.OpenReadStream());
        return Attachment.CreateFile(
            context.workspaceId,
            req.File.FileName,
            req.File.ContentType,
            req.File.Length,
            checksum,
            context.CurrentMember.Id,
            isPublic: false);
    }

    private async Task<Attachment> CreateMediaAttachmentAsync(UploadAttachmentCommand req)
    {
        if (req.File == null) throw new ArgumentNullException(nameof(req.File));
        var checksum = await CalculateChecksumAsync(req.File.OpenReadStream());
        
        return Attachment.CreateMedia(
            context.workspaceId,
            req.File.FileName,
            req.File.ContentType,
            req.File.Length,
            checksum,
            context.CurrentMember.Id,
            isPublic: false);
    }

    private Attachment CreateLinkAttachment(UploadAttachmentCommand req)
    {
        return Attachment.CreateLink(
            context.workspaceId,
            req.Url!,
            req.Title,
            req.Description,
            req.ImageUrl,
            context.CurrentMember.Id,
            isPublic: false);
    }

    private Attachment CreateEmbedAttachment(UploadAttachmentCommand req)
    {
        return Attachment.CreateEmbed(
            context.workspaceId,
            req.Url!,
            req.Provider ?? "Unknown",
            req.Title,
            context.CurrentMember.Id,
            isPublic: false);
    }

    private async Task<string> CalculateChecksumAsync(Stream stream)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        stream.Position = 0;
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
