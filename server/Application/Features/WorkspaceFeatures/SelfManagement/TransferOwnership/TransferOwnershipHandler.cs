using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.WorkspaceFeatures.SelfManagement.TransferOwnership;

public class TransferOwnershipHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken ct)
    {
        // Only Owner can transfer ownership
        if (context.CurrentMember.Role != Domain.Enums.Role.Owner)
            return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only the workspace owner can transfer ownership"));

        if (request.NewOwnerId == context.CurrentMember.UserId)
            return Result.Failure(Error.Validation("Workspace.TransferSameUser", "Cannot transfer ownership to yourself"));

        var workspace = await db.Workspaces
            .ById(context.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        try
        {
            workspace.TransferOwnership(request.NewOwnerId, context.CurrentMember.UserId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.ConditionNotMet with { Description = ex.Message });
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
