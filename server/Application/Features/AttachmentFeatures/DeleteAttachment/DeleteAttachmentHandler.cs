using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteAttachmentHandler(TaskPlanDbContext db, WorkspaceContext context, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(x => x.Id == request.AttachmentId, cancellationToken);
        if (attachment == null) 
            return Result.Failure(AttachmentError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Admin, creatorId: attachment.CreatorId, cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        
        attachment.SoftDelete();
        var affected = await db.SaveChangesAsync(cancellationToken);
        
        if (affected > 0)
        {
            await realtimeService.NotifyEntitiesDeletedAsync(
                context.WorkspaceId,
                new EntityBatchDelete { AttachmentIds = [attachment.Id] },
                cancellationToken);
        }

        return Result.Success();
    }
}


