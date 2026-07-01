using Microsoft.EntityFrameworkCore;

namespace Api;

public class UpdateWorkspaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    ILogger<UpdateWorkspaceHandler> logger
) : ICommandHandler<UpdateWorkspaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await db.ProjectWorkspaces
            .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId && w.DeletedAt == null, cancellationToken);

        if (workspace is null)
            return Result<Guid>.Failure(WorkspaceError.NotFound);

        // Only the workspace creator (owner) can update it.
        // PermissionDecorator already ensured the caller is a member; this narrows to owner.
        if (workspace.CreatorId != workspaceContext.CurrentMember?.UserId)
        {
            logger.LogWarning("User {UserId} is not the owner of workspace {WorkspaceId}", workspaceContext.CurrentMember?.UserId, workspace.Id);
            return Result<Guid>.Failure(MemberError.DontHavePermission);
        }

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        workspace.Update(
            name: request.Name,
            slug: slug,
            description: request.Description,
            color: request.Color,
            icon: request.Icon,
            strictJoin: request.StrictJoin
        );

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated workspace {WorkspaceId}", workspace.Id);
        return Result<Guid>.Success(workspace.Id);
    }
}
