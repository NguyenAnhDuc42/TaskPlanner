using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchUpdateSpaceItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : ICommandHandler<BatchUpdateSpaceItemsCommand>
{
    public async Task<Result> Handle(BatchUpdateSpaceItemsCommand request, CancellationToken ct)
    {
        var taskUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectTask).ToList();
        var folderUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectFolder).ToList();

        return await db.ExecuteInTransactionAsync(async () =>
        {
            // 1. Batch Load all affected Tasks in a single SELECT query
            if (taskUpdates.Any())
            {
                var taskIds = taskUpdates.Select(u => u.Id).ToList();
                var tasks = await db.ProjectTasks
                    .Where(t => taskIds.Contains(t.Id) && t.ProjectWorkspaceId == workspaceContext.workspaceId)
                    .ToListAsync(ct);

                foreach (var update in taskUpdates)
                {
                    var task = tasks.FirstOrDefault(t => t.Id == update.Id);
                    if (task == null)
                        return Result.Failure(Error.NotFound("Task.NotFound", $"Task {update.Id} not found in workspace"));

                    var orderKey = ResolveOrderKey(update.OrderKey, update.PreviousItemOrderKey, update.NextItemOrderKey);

                    // Safely set encapsulated properties via EF Core Entry API
                    if (update.StatusId.HasValue)
                    {
                        var targetStatusId = update.StatusId == Guid.Empty ? null : update.StatusId;
                        db.Entry(task).Property(t => t.StatusId).CurrentValue = targetStatusId;
                    }

                    if (update.Priority != null)
                    {
                        db.Entry(task).Property(t => t.Priority).CurrentValue = Enum.Parse<Priority>(update.Priority);
                    }

                    if (orderKey != null)
                    {
                        db.Entry(task).Property(t => t.OrderKey).CurrentValue = orderKey;
                    }

                    db.Entry(task).Property(t => t.UpdatedAt).CurrentValue = DateTimeOffset.UtcNow;
                }
            }

            // 2. Batch Load all affected Folders in a single SELECT query
            if (folderUpdates.Any())
            {
                var folderIds = folderUpdates.Select(u => u.Id).ToList();
                var folders = await db.ProjectFolders
                    .Where(f => folderIds.Contains(f.Id) && f.ProjectWorkspaceId == workspaceContext.workspaceId)
                    .ToListAsync(ct);

                foreach (var update in folderUpdates)
                {
                    var folder = folders.FirstOrDefault(f => f.Id == update.Id);
                    if (folder == null)
                        return Result.Failure(Error.NotFound("Folder.NotFound", $"Folder {update.Id} not found in workspace"));

                    var orderKey = ResolveOrderKey(update.OrderKey, update.PreviousItemOrderKey, update.NextItemOrderKey);

                    // Safely set encapsulated properties via EF Core Entry API
                    if (update.StatusId.HasValue)
                    {
                        var targetStatusId = update.StatusId == Guid.Empty ? null : update.StatusId;
                        db.Entry(folder).Property(f => f.StatusId).CurrentValue = targetStatusId;
                    }

                    if (update.Priority != null)
                    {
                        db.Entry(folder).Property(f => f.Priority).CurrentValue = Enum.Parse<Priority>(update.Priority);
                    }

                    if (orderKey != null)
                    {
                        db.Entry(folder).Property(f => f.OrderKey).CurrentValue = orderKey;
                    }

                    db.Entry(folder).Property(f => f.UpdatedAt).CurrentValue = DateTimeOffset.UtcNow;
                }
            }

            // 3. Save all changes in a single batch roundtrip
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }, ct);
    }

    // Fully synchronous in-memory index resolution (100% database-free!)
    private string? ResolveOrderKey(string? explicitOrderKey, string? previousItemOrderKey, string? nextItemOrderKey)
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
        if (nextItemOrderKey != null) return FractionalIndex.Before(nextItemOrderKey);

        return null;
    }
}
