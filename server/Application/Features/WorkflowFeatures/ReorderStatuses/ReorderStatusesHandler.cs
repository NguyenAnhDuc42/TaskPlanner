using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.WorkflowFeatures;

public class ReorderStatusesHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) : ICommandHandler<ReorderStatusesCommand>
{
    public async Task<Result> Handle(ReorderStatusesCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin) 
            return Result.Failure(MemberError.DontHavePermission);

        var status = await db.Statuses.FirstOrDefaultAsync(s => s.Id == request.StatusId, ct);
        if (status == null) return Result.Failure(Error.NotFound("Status.NotFound", "Status not found"));

        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, status.WorkflowId, ct);

        var affected = await db.Statuses
            .Where(s => s.Id == request.StatusId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.OrderKey, newOrderKey)
                .SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow), ct);

        if (affected > 0)
        {
            await realtime.NotifyWorkspaceAsync(context.workspaceId, "StatusOrderChanged", new { 
                request.StatusId, 
                NewOrderKey = newOrderKey 
            }, ct);
            return Result.Success();
        }

        return Result.Failure(Error.NotFound("Status.NotFound", "Status not found"));
    }

    private async Task<string> ResolveOrderKey(ReorderStatusesCommand request, Guid workflowId, CancellationToken ct)
    {
        if (request.PreviousStatusOrderKey != null && request.NextStatusOrderKey != null)
        {
            if (string.Compare(request.PreviousStatusOrderKey, request.NextStatusOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(request.PreviousStatusOrderKey);

            return FractionalIndex.Between(request.PreviousStatusOrderKey, request.NextStatusOrderKey);
        }

        if (request.PreviousStatusOrderKey != null) return FractionalIndex.After(request.PreviousStatusOrderKey);
        if (request.NextStatusOrderKey != null) return FractionalIndex.Before(request.NextStatusOrderKey);

        var maxKey = await db.Statuses
            .Where(s => s.WorkflowId == workflowId)
            .MaxAsync(s => s.OrderKey, ct);

        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }
}
