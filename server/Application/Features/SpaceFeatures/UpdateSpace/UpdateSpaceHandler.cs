using Microsoft.EntityFrameworkCore;

namespace Application;

public class UpdateSpaceHandler(TaskPlanDbContext db, WorkspaceContext context, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<UpdateSpaceCommand>
{
    public async Task<Result> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await db.ProjectSpaces.FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, cancellationToken);

        if (space == null) return Result.Failure(SpaceError.NotFound);
        if (space.ProjectWorkspaceId != context.WorkspaceId) return Result.Failure(SpaceError.NotFound);

        var isCreator = space.CreatorId == context.CurrentMember.Id;
        if (!isCreator)
        {
            var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Manager, cancellationToken);
            if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        }

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        space.Update(
            name: request.Name,
            slug: slug,
            color: request.Color,
            icon: request.Icon,
            isPrivate: request.IsPrivate
        );
        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            await realtimeService.NotifyEntitiesUpdatedAsync(
                context.TryGetWorkspaceId().Value,
                new EntityBatchUpdate { Spaces = [SpaceRecord.FromDomain(space, workflowId: null)] },
                cancellationToken);
        }

        return Result.Success();
    }
}



