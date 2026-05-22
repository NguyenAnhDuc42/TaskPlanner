using Microsoft.EntityFrameworkCore;
namespace Application;

public class DeleteSpaceHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtime) 
    : ICommandHandler<DeleteSpaceCommand>
{
    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await db.ProjectSpaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) 
            return Result.Failure(SpaceError.NotFound);

        if (space.ProjectWorkspaceId != context.workspaceId) 
            return Result.Failure(MemberError.DontHavePermission);

        space.Delete();
        
        await db.SaveChangesAsync(ct);
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceDeleting", new { SpaceId = request.SpaceId, WorkspaceId = context.workspaceId }, ct);

        return Result.Success();
    }
}



