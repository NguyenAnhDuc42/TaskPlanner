using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchUpdateItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : ICommandHandler<BatchUpdateItemsCommand>
{
    public async Task<Result> Handle(BatchUpdateItemsCommand request, CancellationToken ct)
    {
        var taskUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectTask).ToList();
        var folderUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectFolder).ToList();

        return await db.ExecuteInTransactionAsync(async () =>
        {
            if (taskUpdates.Any())
            {
                var taskResult = await BatchUpdateTasksAsync(taskUpdates, ct);
                if (taskResult.IsFailure) return taskResult;
            }

            if (folderUpdates.Any()) 
            {
                var folderResult = await BatchUpdateFoldersAsync(folderUpdates, ct);
                if (folderResult.IsFailure) return folderResult;
            }

            return Result.Success();
        }, ct);
    }

    private async Task<Result> BatchUpdateTasksAsync(List<BatchUpdateItemValue> updates, CancellationToken ct)
    {
        foreach (var update in updates)
        {
            var orderKey = await ResolveTaskOrderKeyAsync(update, ct);

            Priority? parsedPriority = update.Priority != null ? Enum.Parse<Priority>(update.Priority) : null;
            Guid? targetStatusId = update.StatusId == Guid.Empty ? null : update.StatusId;
            bool updateStatus = update.StatusId.HasValue;
            bool updatePriority = update.Priority != null;
            bool updateOrder = orderKey != null;

            var affected = await db.ProjectTasks
                .Where(t => t.Id == update.Id && t.ProjectWorkspaceId == workspaceContext.workspaceId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(t => t.StatusId, t => updateStatus ? targetStatusId : t.StatusId)
                    .SetProperty(t => t.Priority, t => updatePriority ? parsedPriority : t.Priority)
                    .SetProperty(t => t.OrderKey, t => updateOrder ? orderKey : t.OrderKey)
                    .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);

            if (affected == 0)
                return Result.Failure(Error.NotFound("Task.NotFound", $"Task {update.Id} not found in workspace"));
        }
        return Result.Success();
    }

    private async Task<Result> BatchUpdateFoldersAsync(List<BatchUpdateItemValue> updates, CancellationToken ct)
    {
        foreach (var update in updates)
        {
            var orderKey = await ResolveFolderOrderKeyAsync(update, ct);

            Priority? parsedPriority = update.Priority != null ? Enum.Parse<Priority>(update.Priority) : null;
            Guid? targetStatusId = update.StatusId == Guid.Empty ? null : update.StatusId;
            bool updateStatus = update.StatusId.HasValue;
            bool updatePriority = update.Priority != null;
            bool updateOrder = orderKey != null;

            var affected = await db.ProjectFolders
                .Where(f => f.Id == update.Id && f.ProjectWorkspaceId == workspaceContext.workspaceId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(f => f.StatusId, f => updateStatus ? targetStatusId : f.StatusId)
                    .SetProperty(f => f.Priority, f => updatePriority ? parsedPriority : f.Priority)
                    .SetProperty(f => f.OrderKey, f => updateOrder ? orderKey : f.OrderKey)
                    .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), ct);

            if (affected == 0)
                return Result.Failure(Error.NotFound("Folder.NotFound", $"Folder {update.Id} not found in workspace"));
        }
        return Result.Success();
    }

    private async Task<string?> ResolveTaskOrderKeyAsync(BatchUpdateItemValue update, CancellationToken ct)
    {
        if (update.OrderKey != null) return update.OrderKey;
        if (update.PreviousItemOrderKey == null && update.NextItemOrderKey == null) return null;

        if (update.PreviousItemOrderKey != null && update.NextItemOrderKey != null)
        {
            if (string.Compare(update.PreviousItemOrderKey, update.NextItemOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(update.PreviousItemOrderKey);

            return FractionalIndex.Between(update.PreviousItemOrderKey, update.NextItemOrderKey);
        }

        if (update.PreviousItemOrderKey != null) return FractionalIndex.After(update.PreviousItemOrderKey);
        if (update.NextItemOrderKey != null) return FractionalIndex.Before(update.NextItemOrderKey);

        var maxKey = await db.ProjectTasks
            .Where(t => t.StatusId == update.StatusId && t.ProjectWorkspaceId == workspaceContext.workspaceId)
            .MaxAsync(t => t.OrderKey, ct);

        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<string?> ResolveFolderOrderKeyAsync(BatchUpdateItemValue update, CancellationToken ct)
    {
        if (update.OrderKey != null) return update.OrderKey;
        if (update.PreviousItemOrderKey == null && update.NextItemOrderKey == null) return null;

        if (update.PreviousItemOrderKey != null && update.NextItemOrderKey != null)
        {
            if (string.Compare(update.PreviousItemOrderKey, update.NextItemOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(update.PreviousItemOrderKey);

            return FractionalIndex.Between(update.PreviousItemOrderKey, update.NextItemOrderKey);
        }

        if (update.PreviousItemOrderKey != null) return FractionalIndex.After(update.PreviousItemOrderKey);
        if (update.NextItemOrderKey != null) return FractionalIndex.Before(update.NextItemOrderKey);

        var maxKey = await db.ProjectFolders
            .Where(f => f.StatusId == update.StatusId && f.ProjectWorkspaceId == workspaceContext.workspaceId)
            .MaxAsync(f => f.OrderKey, ct);

        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }
}


