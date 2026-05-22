using Microsoft.EntityFrameworkCore;
namespace Application;

public class LeaveWorkspaceHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<LeaveWorkspaceCommand>
{
    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await db.ProjectWorkspaces
            .ById(context.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        // Owner cannot leave - must transfer ownership first
        if (workspace.CreatorId == context.CurrentMember.UserId)
            return Result.Failure(Error.Validation("Workspace.OwnerCannotLeave", "Workspace owner cannot leave. Transfer ownership first."));

        workspace.RemoveMembers(new[] { context.CurrentMember.UserId });
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}


