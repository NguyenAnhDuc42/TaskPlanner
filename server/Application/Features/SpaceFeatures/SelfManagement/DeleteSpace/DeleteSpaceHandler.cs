using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public class DeleteSpaceHandler(
    IDataBase db, 
    WorkspaceContext context,
    IBackgroundJobService backgroundJob,
    IRealtimeService realtime
) : ICommandHandler<DeleteSpaceCommand>
{
    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) 
            return Result.Failure(SpaceError.NotFound);

        // Security Resolve: Direct workspace bound check
        if (space.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        // AUTHORIZATION: Only Admin/Owner or the space creator (MemberId) can delete spaces
        if (context.CurrentMember.Role > Role.Admin && space.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        // 1. Logic: Use formal domain method to trigger background cleanup
        space.Delete(context.CurrentMember.Id);
        
        await db.SaveChangesAsync(ct);

        // 2. Instant Trigger for background cleanup (Folders, Tasks, Views)
        backgroundJob.TriggerOutbox();

        // 3. STAGE 1 Notification: UI hides the space immediately
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceDeleting", new { SpaceId = request.SpaceId, WorkspaceId = context.workspaceId }, ct);

        return Result.Success();
    }
}
