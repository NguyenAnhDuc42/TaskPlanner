using Microsoft.EntityFrameworkCore;
namespace Application;

public class UpdateMembersHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<UpdateMembersCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateMembersCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        var members = request.members;
        if (members.Count == 0) return Result<Guid>.Success(context.workspaceId);

        // Using ExecuteUpdate inside a transaction for atomic batch updates
        await db.ExecuteInTransactionAsync(async () =>
        {
            foreach (var member in members)
            {
                await db.WorkspaceMembers
                    .Where(wm => wm.UserId == member.userId && wm.ProjectWorkspaceId == context.workspaceId && wm.DeletedAt == null)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(wm => wm.Role, wm => member.role ?? wm.Role)
                        .SetProperty(wm => wm.Status, wm => member.status ?? wm.Status)
                        .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow), ct);
            }
            return Result.Success();
        }, ct);

        return Result<Guid>.Success(context.workspaceId);
    }
}


