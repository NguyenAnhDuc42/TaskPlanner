using Microsoft.EntityFrameworkCore;

namespace Api;

public class SetWorkspacePinHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    CurrentUserService currentUserService
) : ICommandHandler<SetWorkspacePinCommand, bool>
{
    public async Task<Result<bool>> Handle(SetWorkspacePinCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
            return Result<bool>.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var member = context.CurrentMember;
        if (member == null || context.WorkspaceId != request.WorkspaceId)
        {
            member = await db.WorkspaceMembers
                .Where(m => m.ProjectWorkspaceId == request.WorkspaceId && m.UserId == currentUserId && m.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (member is null)
            return Result<bool>.Failure(Error.Forbidden("Workspace.Forbidden", "You are not a member of this workspace."));

        var memberEntity = await db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.Id == member.Id, cancellationToken);

        if (memberEntity == null)
            return Result<bool>.Failure(MemberError.NotFound);

        memberEntity.SetPinned(request.IsPinned);
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(request.IsPinned);
    }
}
