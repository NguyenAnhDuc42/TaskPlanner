using Microsoft.EntityFrameworkCore;
namespace Application;

public class UpdateMembersHandler(TaskPlanDbContext db, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<UpdateMembersCommand>
{
    public async Task<Result> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin,cancellationToken : cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var memberIds = request.Members.Select(m => m.MemberId).ToHashSet();
        var lookup = request.Members.ToDictionary(m => m.MemberId);

        var workspaceMembers = await db.WorkspaceMembers
            .Include(wm => wm.User)
            .Where(wm => memberIds.Contains(wm.Id) && wm.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var wm in workspaceMembers)
        {
            var update = lookup[wm.Id];
            wm.Update(update.Role, update.Status);
        }
        
        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            var records = workspaceMembers.Select(wm => MemberRecord.FromDomain(wm, wm.User)).ToList();
            if (records.Count > 0)
            {
                await realtimeService.NotifyEntitiesUpdatedAsync(
                    request.WorkspaceId,
                    new EntityBatchUpdate { Members = records },
                    cancellationToken
                );
            }
        }

        return Result.Success();
    }
}


