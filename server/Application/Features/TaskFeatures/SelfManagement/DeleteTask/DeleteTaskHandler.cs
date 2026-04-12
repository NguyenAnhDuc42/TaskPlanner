using Application.Helpers;
using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public class DeleteTaskHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<DeleteTaskCommand>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var task = await db.Tasks
            .ById(request.TaskId)
            .FirstOrDefaultAsync(ct);

        if (task == null) return Result.Failure(TaskError.NotFound);

        // Permission: Admin/Owner or the task creator
        if (context.CurrentMember.Role > Role.Admin && task.CreatorId != context.CurrentMember.UserId)
            return Result.Failure(MemberError.DontHavePermission);

        task.SoftDelete();
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
