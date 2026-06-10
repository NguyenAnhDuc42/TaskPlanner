using Microsoft.EntityFrameworkCore;
namespace Application;

public class TransferOwnershipHandler(TaskPlanDbContext db, WorkspaceContext context, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Owner, cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only the workspace owner can transfer ownership"));

        if (request.NewOwnerId == context.CurrentMember.UserId)
            return Result.Failure(Error.Validation("Workspace.TransferSameUser", "Cannot transfer ownership to yourself"));

        var currentOwner = await db.WorkspaceMembers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == context.CurrentMember.UserId, cancellationToken);
        var newOwner = await db.WorkspaceMembers.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == request.NewOwnerId, cancellationToken);

        if (currentOwner == null || currentOwner.Role != Role.Owner) return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only the workspace owner can transfer ownership"));
        if (newOwner == null) return Result.Failure(Error.Validation("Workspace.NewOwnerNotFound", "New owner not found in workspace"));

        currentOwner.UpdateRole(Role.Admin);
        newOwner.UpdateRole(Role.Owner);

        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            await realtimeService.NotifyEntitiesUpdatedAsync(context.WorkspaceId, new EntityBatchUpdate
            {
                Members = [
                    MemberRecord.FromDomain(currentOwner, currentOwner.User),
                    MemberRecord.FromDomain(newOwner, newOwner.User)
                ]
            }, cancellationToken);
        }

        return Result.Success();
    }
}



