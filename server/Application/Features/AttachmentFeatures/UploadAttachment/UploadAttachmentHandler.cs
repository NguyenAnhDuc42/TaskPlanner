using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.AttachmentFeatures.UploadAttachment;

public class UploadAttachmentHandler : BaseFeatureHandler, IRequestHandler<UploadAttachmentCommand, Unit>
{
    public UploadAttachmentHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        Attachment attachment = request.Type switch
        {
            AttachmentType.File => await CreateFileAttachmentAsync(request),
            AttachmentType.Media => await CreateMediaAttachmentAsync(request),
            AttachmentType.Link => CreateLinkAttachment(request),
            AttachmentType.Embed => CreateEmbedAttachment(request),
            _ => throw new ArgumentException("Invalid Attachment Type")
        };

        // 2. Persist Metadata
        UnitOfWork.Set<Attachment>().Add(attachment);

        // 3. Create the Relation (The Link)
        var link = AttachmentLink.Create(
            attachment.Id,
            request.ParentEntityId,
            request.EntityType,
            CurrentUserId);

        UnitOfWork.Set<AttachmentLink>().Add(link);


        // 4. Trigger Background Processing if needed
        //if (attachment.Type is AttachmentType.File or AttachmentType.Media)
        //{
        //    _backgroundJobs.Enqueue<IUploadJob>(j => j.ProcessAsync(attachment.Id, request.File));
        //}

        return Unit.Value;
    }
    private async Task<Attachment> CreateFileAttachmentAsync(UploadAttachmentCommand req)
    {
        if (req.File == null) throw new ArgumentNullException(nameof(req.File));
        var checksum = await CalculateChecksumAsync(req.File.OpenReadStream());
        return Attachment.CreateFile(
            req.File.FileName,
            req.File.ContentType,
            req.File.Length,
            checksum,
            CurrentUserId);
    }

    private async Task<Attachment> CreateMediaAttachmentAsync(UploadAttachmentCommand req)
    {
        if (req.File == null) throw new ArgumentNullException(nameof(req.File));
        var checksum = await CalculateChecksumAsync(req.File.OpenReadStream());
        // Media starts as Pending; the Background Job will update Width/Height later
        return Attachment.CreateMedia(
            req.File.FileName,
            req.File.ContentType,
            req.File.Length,
            checksum,
            CurrentUserId);
    }

    private Attachment CreateLinkAttachment(UploadAttachmentCommand req)
    {
        // For Links, we expect Url and Title in the Command
        return Attachment.CreateLink(
            req.Url!,
            req.Title,
            req.Description,
            req.ImageUrl,
            CurrentUserId);
    }

    private Attachment CreateEmbedAttachment(UploadAttachmentCommand req)
    {
        return Attachment.CreateEmbed(
            req.Url!,
            req.Provider ?? "Unknown",
            req.Title,
            CurrentUserId);
    }

    private async Task<string> CalculateChecksumAsync(Stream stream)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        stream.Position = 0;
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

}
