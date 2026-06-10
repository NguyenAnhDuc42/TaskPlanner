using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteAttachmentHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(x => x.Id == request.AttachmentId, cancellationToken);
        if (attachment == null) 
            return Result.Failure(AttachmentError.NotFound);

        if (context.CurrentMember.Role > Role.Admin && attachment.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);
        
        attachment.SoftDelete();
        await db.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}


