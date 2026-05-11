using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.TaskFeatures;

public class MoveTaskToStatusHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) : ICommandHandler<MoveTaskToStatusCommand>
{
    public async Task<Result> Handle(MoveTaskToStatusCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin) 
            return Result.Failure(MemberError.DontHavePermission);

        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, ct);

        var taskData = await db.Tasks
            .Where(t => t.Id == request.TaskId)
            .Select(t => new { t.ProjectSpaceId, t.ProjectFolderId })
            .FirstOrDefaultAsync(ct);

        var affected = await db.Tasks
            .Where(t => t.Id == request.TaskId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(t => t.StatusId, request.TargetStatusId)
                .SetProperty(t => t.OrderKey, newOrderKey)
                .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);

        if (affected > 0)
        {
            await realtime.NotifyWorkspaceAsync(context.workspaceId, "TaskStatusChanged", new { 
                request.TaskId, 
                request.TargetStatusId, 
                NewOrderKey = newOrderKey,
                SpaceId = taskData?.ProjectSpaceId,
                FolderId = taskData?.ProjectFolderId
            }, ct);
            return Result.Success();
        }

        return Result.Failure(Error.NotFound("Task.NotFound", "Task not found"));
    }

    private async Task<string> ResolveOrderKey(MoveTaskToStatusCommand request, CancellationToken ct)
    {
        if (request.PreviousItemOrderKey != null && request.NextItemOrderKey != null)
        {
            if (string.Compare(request.PreviousItemOrderKey, request.NextItemOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(request.PreviousItemOrderKey);

            return FractionalIndex.Between(request.PreviousItemOrderKey, request.NextItemOrderKey);
        }

        if (request.PreviousItemOrderKey != null) return FractionalIndex.After(request.PreviousItemOrderKey);
        if (request.NextItemOrderKey != null) return FractionalIndex.Before(request.NextItemOrderKey);

        var maxKey = await db.Tasks
            .Where(t => t.StatusId == request.TargetStatusId)
            .MaxAsync(t => t.OrderKey, ct);

        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }
}
