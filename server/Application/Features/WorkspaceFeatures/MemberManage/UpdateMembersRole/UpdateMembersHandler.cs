using Microsoft.EntityFrameworkCore;
namespace Application;

public class UpdateMembersHandler(TaskPlanDbContext db, WorkspaceContext context,PermissionService permissionService) : ICommandHandler<UpdateMembersCommand>
{
    public async Task<Result> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin,cancellationToken : cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var memberIds = request.Members.Select(m => m.MemberId).ToHashSet();
        var lookup = request.Members.ToDictionary(m => m.MemberId);

        var workspaceMembers = await db.WorkspaceMembers
            .Where(wm => memberIds.Contains(wm.Id)
                      && wm.ProjectWorkspaceId == context.TryGetWorkspaceId().Value
                      && wm.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var wm in workspaceMembers)
        {
            var update = lookup[wm.Id];
            wm.Update(update.Role, update.Status);
        }
        
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


