using Microsoft.EntityFrameworkCore;

namespace Application;

public class RemoveMembersHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<RemoveMembersCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        if (request.memberIds.Any())
        {
            await db.WorkspaceMembers
                .Where(wm => wm.ProjectWorkspaceId == context.workspaceId 
                          && request.memberIds.Contains(wm.UserId) 
                          && wm.DeletedAt == null)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                    .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        }

        return Result<Guid>.Success(context.workspaceId);
    }
}


