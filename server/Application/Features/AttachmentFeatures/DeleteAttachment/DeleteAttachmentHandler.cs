using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.AttachmentFeatures;

public class DeleteAttachmentHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(x => x.Id == request.AttachmentId, ct);
        if (attachment == null) 
            return Result.Failure(AttachmentError.NotFound);

        if (context.CurrentMember.Role > Role.Admin && attachment.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);
        
        attachment.SoftDelete();
        await db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
