using Microsoft.EntityFrameworkCore;

namespace Api;

public class LeaveWorkspaceHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<LeaveWorkspaceCommand>
{
    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await db.ProjectWorkspaces.FirstOrDefaultAsync(w => w.Id == context.TryGetWorkspaceId().Value, cancellationToken);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        if (workspace.CreatorId == context.CurrentMember.UserId)
            return Result.Failure(Error.Validation("Workspace.OwnerCannotLeave", "Workspace owner cannot leave. Transfer ownership first."));

        workspace.RemoveMembers(new[] { context.CurrentMember.UserId });
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
