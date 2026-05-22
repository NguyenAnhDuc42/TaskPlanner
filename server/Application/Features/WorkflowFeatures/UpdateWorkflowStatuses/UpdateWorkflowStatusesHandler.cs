using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces;

namespace Application.Features.WorkflowFeatures;

public class UpdateWorkflowStatusesHandler(IDataBase db, WorkspaceContext context, HybridCache cache, IRealtimeService realtime) 
    : ICommandHandler<UpdateWorkflowStatusesCommand>
{
    public async Task<Result> Handle(UpdateWorkflowStatusesCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workflow = await db.Workflows
            .Include(w => w.Statuses)
            .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, ct);

        if (workflow == null) return Result.Failure(WorkflowError.NotFound);

        var existingStatuses = workflow.Statuses.ToDictionary(s => s.Id);

        var existingModifiers = request.Statuses.Where(s => s.Action == RowAction.Update || s.Action == RowAction.Delete);
        var creates = request.Statuses.Where(s => s.Action == RowAction.Create);

        // 1. Process Updates and Deletes
        foreach (var statusDto in existingModifiers)
        {
            if (statusDto.Id.HasValue && existingStatuses.TryGetValue(statusDto.Id.Value, out var existing))
            {
                if (statusDto.Action == RowAction.Delete)
                {
                    workflow.RemoveStatus(existing.Id);
                }
                else
                {
                    var resolvedKey = (statusDto.PreviousOrderKey != null || statusDto.NextOrderKey != null)
                        ? ResolveOrderKey(statusDto.PreviousOrderKey, statusDto.NextOrderKey)
                        : null;

                    existing.Update(
                        name: statusDto.Name,
                        color: statusDto.Color,
                        category: statusDto.Category,
                        orderKey: resolvedKey
                    );
                }
            }
        }

        // 3. Process Creates
        foreach (var statusDto in creates)
        {
            var orderKey = (statusDto.PreviousOrderKey != null || statusDto.NextOrderKey != null)
                ? ResolveOrderKey(statusDto.PreviousOrderKey, statusDto.NextOrderKey)
                : null;
            
            var @new = Status.Create(
                workflow.ProjectWorkspaceId, 
                workflow.Id, 
                statusDto.Name, 
                statusDto.Color!, 
                statusDto.Category, 
                context.CurrentMember.Id,
                orderKey);
            
            workflow.AddStatus(@new);
        }

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync($"Workflows-{context.workspaceId}", ct);
        await cache.RemoveByTagAsync($"Statuses-{context.workspaceId}", ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "WorkflowUpdated", new { WorkflowId = workflow.Id }, ct);

        return Result.Success();
    }

    private static string ResolveOrderKey(string? prevKey, string? nextKey)
    {
        if (prevKey != null && nextKey != null)
        {
            if (string.Compare(prevKey, nextKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(prevKey);

            return FractionalIndex.Between(prevKey, nextKey);
        }

        if (prevKey != null) return FractionalIndex.After(prevKey);
        if (nextKey != null) return FractionalIndex.Before(nextKey);

        return FractionalIndex.Start();
    }
}
