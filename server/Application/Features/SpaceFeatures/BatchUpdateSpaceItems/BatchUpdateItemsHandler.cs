using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchUpdateSpaceItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : ICommandHandler<BatchUpdateSpaceItemsCommand>
{
    public async Task<Result> Handle(BatchUpdateSpaceItemsCommand request, CancellationToken cancellationToken)
    {
        var taskUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectTask).ToList();
        var folderUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectFolder).ToList();

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var taskResult = await ProcessTaskUpdates(taskUpdates, cancellationToken);
            if (taskResult.IsFailure) return taskResult;

            var folderResult = await ProcessFolderUpdates(folderUpdates, cancellationToken);
            if (folderResult.IsFailure) return folderResult;

            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }

    private async Task<Result> ProcessTaskUpdates(List<BatchUpdateSpaceItemValue> taskUpdates, CancellationToken cancellationToken)
    {
        if (!taskUpdates.Any())
            return Result.Success();

        var taskIds = taskUpdates.Select(u => u.Id).ToList();
        var taskMap = await db.ProjectTasks
            .Where(t => taskIds.Contains(t.Id) && t.ProjectWorkspaceId == workspaceContext.workspaceId)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        foreach (var update in taskUpdates)
        {
            if (!taskMap.TryGetValue(update.Id, out var task))
                return Result.Failure(Error.NotFound("Task.NotFound", $"Task {update.Id} not found in workspace"));

            var applyResult = ApplyTaskUpdate(task, update);
            if (applyResult.IsFailure) return applyResult;
        }

        return Result.Success();
    }

    private async Task<Result> ProcessFolderUpdates(List<BatchUpdateSpaceItemValue> folderUpdates, CancellationToken cancellationToken)
    {
        if (!folderUpdates.Any())
            return Result.Success();

        var folderIds = folderUpdates.Select(u => u.Id).ToList();
        var folderMap = await db.ProjectFolders
            .Where(f => folderIds.Contains(f.Id) && f.ProjectWorkspaceId == workspaceContext.workspaceId)
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        foreach (var update in folderUpdates)
        {
            if (!folderMap.TryGetValue(update.Id, out var folder))
                return Result.Failure(Error.NotFound("Folder.NotFound", $"Folder {update.Id} not found in workspace"));

            var applyResult = ApplyFolderUpdate(folder, update);
            if (applyResult.IsFailure) return applyResult;
        }

        return Result.Success();
    }

    private Result ApplyTaskUpdate(ProjectTask task, BatchUpdateSpaceItemValue update)
    {
        if (update.StatusId.HasValue)
            db.Entry(task).Property(t => t.StatusId).CurrentValue = update.StatusId == Guid.Empty ? null : update.StatusId;

        if (update.Priority != null)
        {
            if (!Enum.TryParse<Priority>(update.Priority, out var priority))
                return Result.Failure(Error.Validation("Priority.Invalid", $"'{update.Priority}' is not a valid priority"));

            db.Entry(task).Property(t => t.Priority).CurrentValue = priority;
        }

        var orderKey = ResolveOrderKey(update.OrderKey, update.PreviousItemOrderKey, update.NextItemOrderKey);
        if (orderKey != null)
            db.Entry(task).Property(t => t.OrderKey).CurrentValue = orderKey;

        db.Entry(task).Property(t => t.UpdatedAt).CurrentValue = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    private Result ApplyFolderUpdate(ProjectFolder folder, BatchUpdateSpaceItemValue update)
    {
        if (update.StatusId.HasValue)
            db.Entry(folder).Property(f => f.StatusId).CurrentValue = update.StatusId == Guid.Empty ? null : update.StatusId;

        if (update.Priority != null)
        {
            if (!Enum.TryParse<Priority>(update.Priority, out var priority))
                return Result.Failure(Error.Validation("Priority.Invalid", $"'{update.Priority}' is not a valid priority"));

            db.Entry(folder).Property(f => f.Priority).CurrentValue = priority;
        }

        var orderKey = ResolveOrderKey(update.OrderKey, update.PreviousItemOrderKey, update.NextItemOrderKey);
        if (orderKey != null)
            db.Entry(folder).Property(f => f.OrderKey).CurrentValue = orderKey;

        db.Entry(folder).Property(f => f.UpdatedAt).CurrentValue = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    private static string? ResolveOrderKey(string? explicitOrderKey, string? previousItemOrderKey, string? nextItemOrderKey)
    {
        if (explicitOrderKey != null) return explicitOrderKey;
        if (previousItemOrderKey == null && nextItemOrderKey == null) return null;

        if (previousItemOrderKey != null && nextItemOrderKey != null)
        {
            if (string.Compare(previousItemOrderKey, nextItemOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(previousItemOrderKey);

            return FractionalIndex.Between(previousItemOrderKey, nextItemOrderKey);
        }

        if (previousItemOrderKey != null) return FractionalIndex.After(previousItemOrderKey);

        return FractionalIndex.Before(nextItemOrderKey!);
    }
}