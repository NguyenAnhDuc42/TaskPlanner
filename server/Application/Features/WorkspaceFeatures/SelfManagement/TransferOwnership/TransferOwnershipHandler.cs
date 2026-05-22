using Microsoft.EntityFrameworkCore;
namespace Application;

public class TransferOwnershipHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken ct)
    {
        // Only Owner can transfer ownership
        if (context.CurrentMember.Role != Role.Owner)
            return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only the workspace owner can transfer ownership"));

        if (request.NewOwnerId == context.CurrentMember.UserId)
            return Result.Failure(Error.Validation("Workspace.TransferSameUser", "Cannot transfer ownership to yourself"));

        var currentOwner = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.UserId == context.CurrentMember.UserId && m.ProjectWorkspaceId == context.workspaceId, ct);
        var newOwner = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.UserId == request.NewOwnerId && m.ProjectWorkspaceId == context.workspaceId, ct);

        if (currentOwner == null || currentOwner.Role != Role.Owner) return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only the workspace owner can transfer ownership"));
        if (newOwner == null) return Result.Failure(Error.Validation("Workspace.NewOwnerNotFound", "New owner not found in workspace"));

        currentOwner.UpdateRole(Role.Admin);
        newOwner.UpdateRole(Role.Owner);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}



