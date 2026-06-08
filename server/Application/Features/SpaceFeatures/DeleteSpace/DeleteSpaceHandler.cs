using Microsoft.EntityFrameworkCore;
namespace Application;

public class DeleteSpaceHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtimeService, PermissionService permissionService)
    : ICommandHandler<DeleteSpaceCommand>
{
    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await db.ProjectSpaces.FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, ct);

        if (space == null) return Result.Failure(SpaceError.NotFound);
        if (space.ProjectWorkspaceId != context.WorkspaceId) return Result.Failure(SpaceError.NotFound);

        var isCreator = space.CreatorId == context.CurrentMember.Id;
        if (!isCreator)
        {
            var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Manager, ct);
            if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        }
        space.Delete();


        var affectedRows = await db.SaveChangesAsync(ct);
        if (affectedRows > 0)
        {
            await realtimeService.NotifyEntitiesDeletedAsync(
                context.TryGetWorkspaceId().Value,
                new EntityBatchDelete { SpaceIds = [space.Id] },
                ct);
        }

        return Result.Success();
    }
}



