using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class RemoveMembersHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<RemoveMembersCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RemoveMembersCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        if (request.memberIds.Any())
        {
            await db.Database.GetDbConnection().ExecuteAsync(RemoveMembersSQL.RemoveMembers, new
            {
                WorkspaceId = context.workspaceId,
                UserIds = request.memberIds.ToArray()
            });
        }

        return Result<Guid>.Success(context.workspaceId);
    }
}


