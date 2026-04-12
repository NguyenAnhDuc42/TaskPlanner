using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.SelfManagement.LeaveWorkspace;

public class LeaveWorkspaceHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<LeaveWorkspaceCommand>
{
    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken ct)
    {
        var workspace = await db.Workspaces
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
