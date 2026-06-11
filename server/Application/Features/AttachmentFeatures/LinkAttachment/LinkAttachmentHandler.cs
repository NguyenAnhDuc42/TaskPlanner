using Microsoft.EntityFrameworkCore;

namespace Application;

public class LinkAttachmentHandler(TaskPlanDbContext db, WorkspaceContext context, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<LinkAttachmentCommand>
{
    public async Task<Result> Handle(LinkAttachmentCommand request, CancellationToken cancellationToken)
    {
        // AUTHORIZATION: Only Member or above can link attachments to entities
        var hasAccess = await permissionService.VerifyAsync(Role.Member, cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var attachment = await db.Attachments.FirstOrDefaultAsync(x => x.Id == request.AttachmentId, cancellationToken);
        if (attachment == null) 
            return Result.Failure(AttachmentError.NotFound);
        
        var link = EntityAssetLink.Create(
            context.WorkspaceId,
            attachment.Id,
            AssetType.Attachment,
            request.ParentEntityType == EntityType.ProjectSpace ? request.ParentEntityId : null,
            request.ParentEntityType == EntityType.ProjectFolder ? request.ParentEntityId : null,
            request.ParentEntityType == EntityType.ProjectTask ? request.ParentEntityId : null,
            request.ParentEntityType == EntityType.Comment ? request.ParentEntityId : null,
            context.CurrentMember.Id);

        await db.EntityAssetLinks.AddAsync(link, cancellationToken);
        var affected = await db.SaveChangesAsync(cancellationToken);
        
        if (affected > 0)
        {
            await realtimeService.NotifyEntitiesUpdatedAsync(
                context.WorkspaceId,
                new EntityBatchUpdate { Attachments = [AttachmentRecord.FromDomain(attachment, link)] },
                cancellationToken);
        }

        return Result.Success();
    }
}


