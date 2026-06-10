using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchUpdateSpaceItemsHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realTimeService) : ICommandHandler<BatchUpdateSpaceItemsCommand>
{
     public async Task<Result> Handle(BatchUpdateSpaceItemsCommand request, CancellationToken cancellationToken)
    {
        var space = await db.ProjectSpaces
            .AsNoTracking()
            .Where(s => s.Id == request.SpaceId 
                    && s.DeletedAt == null)
            .Select(s => new { s.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);

        if (space is null) return Result.Failure(SpaceError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Editor, space.CreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var taskUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectTask).ToList();
        var folderUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectFolder).ToList();

        List<TaskRecord> taskRecords = [];
        List<FolderRecord> folderRecords = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var (taskResult, tasks) = await ProcessTaskUpdates(taskUpdates, cancellationToken);
            if (taskResult.IsFailure) return taskResult;

            var (folderResult, folders) = await ProcessFolderUpdates(folderUpdates, cancellationToken);
            if (folderResult.IsFailure) return folderResult;

            await db.SaveChangesAsync(cancellationToken);

            taskRecords = tasks;
            folderRecords = folders;

            return Result.Success();
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await realTimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate
                {
                    Tasks = taskRecords.Count > 0 ? taskRecords : null,
                    Folders = folderRecords.Count > 0 ? folderRecords : null
                },
                cancellationToken);
        }

        return result;
    }

    private async Task<(Result, List<TaskRecord>)> ProcessTaskUpdates(List<BatchUpdateSpaceItemValue> taskUpdates, CancellationToken cancellationToken)
    {
        if (taskUpdates.Count == 0) return (Result.Success(), []);

        var taskIds = taskUpdates.Select(u => u.Id).ToList();
        var taskMap = await db.ProjectTasks
            .Where(t => taskIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var records = new List<TaskRecord>();
        foreach (var update in taskUpdates)
        {
            if (!taskMap.TryGetValue(update.Id, out var task))
                return (Result.Failure(Error.NotFound("Task.NotFound", $"Task {update.Id} not found")), []);

            var orderKey = ResolveOrderKey(update.OrderKey, update.PreviousItemOrderKey, update.NextItemOrderKey);
            task.Update(statusId: update.StatusId, priority: update.Priority, orderKey: orderKey);
            records.Add(TaskRecord.FromDomain(task));
        }

        return (Result.Success(), records);
    }

    private async Task<(Result, List<FolderRecord>)> ProcessFolderUpdates(List<BatchUpdateSpaceItemValue> folderUpdates, CancellationToken cancellationToken)
    {
        if (folderUpdates.Count == 0) return (Result.Success(), []);

        var folderIds = folderUpdates.Select(u => u.Id).ToList();
        var folderMap = await db.ProjectFolders
            .Where(f => folderIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);

        var records = new List<FolderRecord>();
        foreach (var update in folderUpdates)
        {
            if (!folderMap.TryGetValue(update.Id, out var folder))
                return (Result.Failure(Error.NotFound("Folder.NotFound", $"Folder {update.Id} not found")), []);

            var orderKey = ResolveOrderKey(update.OrderKey, update.PreviousItemOrderKey, update.NextItemOrderKey);
            folder.Update(statusId: update.StatusId, priority: update.Priority, orderKey: orderKey);
            records.Add(FolderRecord.FromDomain(folder));
        }

        return (Result.Success(), records);
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