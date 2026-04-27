using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AttachmentFeatures;

public class LinkAttachmentHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<LinkAttachmentCommand>
{
    public async Task<Result> Handle(LinkAttachmentCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Member or above can link attachments to entities
        if (context.CurrentMember.Role > Role.Member)
            return Result.Failure(MemberError.DontHavePermission);

        var attachment = await db.Attachments.FirstOrDefaultAsync(x => x.Id == request.AttachmentId, ct);
        if (attachment == null) 
            return Result.Failure(AttachmentError.NotFound);
        
        var link = EntityAssetLink.Create(
            context.workspaceId,
            attachment.Id,
            AssetType.Attachment,
            request.ParentEntityType == EntityType.ProjectSpace ? request.ParentEntityId : null,
            request.ParentEntityType == EntityType.ProjectFolder ? request.ParentEntityId : null,
            request.ParentEntityType == EntityType.ProjectTask ? request.ParentEntityId : null,
            request.ParentEntityType == EntityType.Comment ? request.ParentEntityId : null,
            context.CurrentMember.Id);

        await db.EntityAssetLinks.AddAsync(link, ct);
        await db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
