using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.SpaceFeatures;

public class DeleteSpaceHandler(IDataBase db, WorkspaceContext context, IBackgroundJobService backgroundJob, IRealtimeService realtime) 
    : ICommandHandler<DeleteSpaceCommand>
{
    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) 
            return Result.Failure(SpaceError.NotFound);

        if (space.ProjectWorkspaceId != context.workspaceId) 
            return Result.Failure(MemberError.DontHavePermission);

        if (context.CurrentMember.Role > Role.Admin && space.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        space.Delete(context.CurrentMember.Id);
        
        await db.SaveChangesAsync(ct);
        backgroundJob.TriggerOutbox();
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceDeleting", new { SpaceId = request.SpaceId, WorkspaceId = context.workspaceId }, ct);

        return Result.Success();
    }
}
