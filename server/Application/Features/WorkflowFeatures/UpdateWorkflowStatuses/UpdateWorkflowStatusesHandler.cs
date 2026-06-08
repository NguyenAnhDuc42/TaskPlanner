using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application;

public class UpdateWorkflowStatusesHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    HybridCache cache,
    RealtimeService realtime)
    : ICommandHandler<UpdateWorkflowStatusesCommand>
{
    public async Task<Result> Handle(
        UpdateWorkflowStatusesCommand request,
        CancellationToken cancellationToken)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workflow = await db.Workflows
            .Include(x => x.Statuses)
            .FirstOrDefaultAsync(
                x => x.Id == request.WorkflowId,
                cancellationToken);

        if (workflow is null)
            return Result.Failure(WorkflowError.NotFound);

        var existingStatuses = workflow.Statuses
            .ToDictionary(x => x.Id);

        foreach (var status in request.Statuses)
        {
            switch (status.Action)
            {
                case RowAction.Create:
                    ProcessCreate(status, workflow);
                    break;

                case RowAction.Update:
                    if (status.Id is null)
                        continue;

                    if (!existingStatuses.TryGetValue(
                            status.Id.Value,
                            out var statusToUpdate))
                        continue;

                    ProcessUpdate(statusToUpdate, status);
                    break;

                case RowAction.Delete:
                    if (status.Id is null)
                        continue;

                    if (!existingStatuses.TryGetValue(
                            status.Id.Value,
                            out var statusToDelete))
                        continue;

                    ProcessDelete(statusToDelete, workflow);
                    break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(
            $"Workflows-{context.WorkspaceId}",
            cancellationToken);

        await cache.RemoveByTagAsync(
            $"Statuses-{context.WorkspaceId}",
            cancellationToken);

        await realtime.NotifyWorkspaceAsync(
            context.WorkspaceId,
            "WorkflowUpdated",
            new { WorkflowId = workflow.Id },
            cancellationToken);

        return Result.Success();
    }

    private void ProcessCreate(
        StatusUpdateValue dto,
        Workflow workflow)
    {
        var orderKey = ResolveOrderKey(
            dto.PreviousOrderKey,
            dto.NextOrderKey);

        var status = Status.Create(
            workflow.ProjectWorkspaceId,
            workflow.Id,
            dto.Name,
            dto.Color,
            dto.Category,
            context.CurrentMember.Id,
            orderKey);

        workflow.AddStatus(status);
    }

    private void ProcessDelete(
        Status status,
        Workflow workflow)
    {
        workflow.RemoveStatus(status.Id);
        db.Statuses.Remove(status);
    }

    private static void ProcessUpdate(
        Status status,
        StatusUpdateValue dto)
    {
        var orderKey = ResolveOrderKey(
            dto.PreviousOrderKey,
            dto.NextOrderKey);

        status.Update(
            dto.Name,
            dto.Color,
            dto.Category,
            orderKey);
    }

    private static string? ResolveOrderKey(
        string? previousKey,
        string? nextKey)
    {
        if (previousKey is null && nextKey is null)
            return null;

        if (previousKey is not null && nextKey is not null)
        {
            return string.Compare(
                       previousKey,
                       nextKey,
                       StringComparison.Ordinal) >= 0
                ? FractionalIndex.After(previousKey)
                : FractionalIndex.Between(previousKey, nextKey);
        }

        if (previousKey is not null)
            return FractionalIndex.After(previousKey);

        return FractionalIndex.Before(nextKey!);
    }
}