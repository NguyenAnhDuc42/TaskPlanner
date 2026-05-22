using Dapper;
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

        await db.Database.GetDbConnection().ExecuteAsync(UpdateMembersSQL.UpdateMemberRoles, new
        {
            UserIds = members.Select(m => m.userId).ToArray(),
            Roles = members.Select(m => m.role?.ToString() ?? string.Empty).ToArray(),
            Statuses = members.Select(m => m.status?.ToString() ?? string.Empty).ToArray(),
            WorkspaceId = context.workspaceId
        });

        return Result<Guid>.Success(context.workspaceId);
    }
}


