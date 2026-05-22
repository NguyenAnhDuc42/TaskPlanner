using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application;

public class AddMembersHandler(TaskPlanDbContext db, WorkspaceContext context) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.ProjectWorkspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        var members = request.members;
        if (!members.Any()) return Result.Success();

        await db.Database.GetDbConnection().ExecuteAsync(AddMembersSQL.BulkAddMembers, new
        {
            WorkspaceId = workspace.Id,
            CreatorId = context.CurrentMember.Id,
            Emails = members.Select(m => m.email).ToArray(),
            Roles = members.Select(m => m.role.ToString()).ToArray(),
            Theme = Theme.Dark.ToString()
        });


        return Result.Success();
    }
}



