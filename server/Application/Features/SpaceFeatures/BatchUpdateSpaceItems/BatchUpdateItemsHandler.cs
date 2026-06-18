using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Application;

public class BatchUpdateSpaceItemsHandler(
    TaskPlanDbContext db, 
    WorkspaceContext workspaceContext, 
    PermissionService permissionService, 
    RealtimeService realTimeService,
    ILogger<BatchUpdateSpaceItemsHandler> logger) 
    : ICommandHandler<BatchUpdateSpaceItemsCommand>
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

        if (request.Updates is not { Count: > 0 }) return Result.Success();

        List<TaskRecord> taskRecords = [];
        List<FolderRecord> folderRecords = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var applyResult = await ApplyBatchAsync(request, cancellationToken);
            if (applyResult.IsFailure) return applyResult;
            var taskIds = request.Updates.Where(u => u.Type == EntityLayerType.ProjectTask).Select(u => u.Id).ToList();
            var folderIds = request.Updates.Where(u => u.Type == EntityLayerType.ProjectFolder).Select(u => u.Id).ToList();

            if (taskIds.Count > 0)
            {
                taskRecords = (await db.ProjectTasks
                    .AsNoTracking()
                    .Where(t => taskIds.Contains(t.Id))
                    .ToListAsync(cancellationToken))
                    .Select(TaskRecord.FromDomain)
                    .ToList();
            }

            if (folderIds.Count > 0)
            {
                var workflowIdsMap = await db.Workflows
                    .Where(w => w.ProjectFolderId != null && folderIds.Contains(w.ProjectFolderId.Value))
                    .ToDictionaryAsync(w => w.ProjectFolderId!.Value, w => (Guid?)w.Id, cancellationToken);

                var folders = await db.ProjectFolders
                    .AsNoTracking()
                    .Where(f => folderIds.Contains(f.Id))
                    .ToListAsync(cancellationToken);

                foreach (var folder in folders)
                {
                    workflowIdsMap.TryGetValue(folder.Id, out var workflowId);
                    folderRecords.Add(FolderRecord.FromDomain(folder, workflowId));
                }
            }

            return Result.Success();
        }, cancellationToken);

        if (result.IsSuccess)
        {
            _ =  realTimeService
            .NotifyEntitiesUpdatedAsync(workspaceContext.WorkspaceId,
                new EntityBatchUpdate
                {
                    Tasks = taskRecords.Count > 0 ? taskRecords : null,
                    Folders = folderRecords.Count > 0 ? folderRecords : null
                }, default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for batch updated items in space {SpaceId}", request.SpaceId), 
                TaskContinuationOptions.OnlyOnFaulted);
        }
        return result;
    }

    private async Task<Result> ApplyBatchAsync(BatchUpdateSpaceItemsCommand request, CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        var workspaceId = workspaceContext.WorkspaceId;

        var taskUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectTask).ToList();
        var folderUpdates = request.Updates.Where(u => u.Type == EntityLayerType.ProjectFolder).ToList();

        if (taskUpdates.Count > 0)
        {
            var requestedIds = taskUpdates.Select(u => u.Id).ToArray();
            var existingIds = (await connection.QueryAsync<Guid>(
                @"SELECT id FROM project_tasks 
                  WHERE id = ANY(@Ids) AND project_workspace_id = @WorkspaceId AND deleted_at IS NULL",
                new { Ids = requestedIds, WorkspaceId = workspaceId }
            )).ToHashSet();

            var missing = requestedIds.FirstOrDefault(id => !existingIds.Contains(id));
            if (missing != Guid.Empty)
                return Result.Failure(Error.NotFound("Task.NotFound", $"Task {missing} not found"));

            await ApplyTaskOrderKeyBatch(connection, taskUpdates, workspaceId, cancellationToken);
            await ApplyTaskStatusBatch(connection, taskUpdates, workspaceId, cancellationToken);
            await ApplyTaskPriorityBatch(connection, taskUpdates, workspaceId, cancellationToken);
        }

        if (folderUpdates.Count > 0)
        {
            var requestedIds = folderUpdates.Select(u => u.Id).ToArray();
            var existingIds = (await connection.QueryAsync<Guid>(
                @"SELECT id FROM project_folders 
                  WHERE id = ANY(@Ids) AND project_workspace_id = @WorkspaceId AND deleted_at IS NULL",
                new { Ids = requestedIds, WorkspaceId = workspaceId }
            )).ToHashSet();

            var missing = requestedIds.FirstOrDefault(id => !existingIds.Contains(id));
            if (missing != Guid.Empty)
                return Result.Failure(Error.NotFound("Folder.NotFound", $"Folder {missing} not found"));

            await ApplyFolderOrderKeyBatch(connection, folderUpdates, workspaceId, cancellationToken);
            await ApplyFolderStatusBatch(connection, folderUpdates, workspaceId, cancellationToken);
            await ApplyFolderPriorityBatch(connection, folderUpdates, workspaceId, cancellationToken);
        }

        return Result.Success();
    }

    private static async Task ApplyTaskOrderKeyBatch(DbConnection connection, List<BatchUpdateSpaceItemValue> updates, Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = updates.Select(u => new { u.Id, OrderKey = ResolveOrderKey(u.OrderKey, u.PreviousItemOrderKey, u.NextItemOrderKey) })
                          .Where(x => x.OrderKey is not null)
                          .ToArray();
        if (rows.Length == 0) return;

        await connection.ExecuteAsync(new CommandDefinition(
            @"UPDATE project_tasks t SET order_key = v.order_key, updated_at = NOW()
              FROM UNNEST(@Ids, @OrderKeys) AS v(id, order_key)
              WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
            new { Ids = rows.Select(r => r.Id).ToArray(), OrderKeys = rows.Select(r => r.OrderKey).ToArray(), WorkspaceId = workspaceId },
            cancellationToken: cancellationToken));
    }

    private static async Task ApplyFolderOrderKeyBatch(DbConnection connection, List<BatchUpdateSpaceItemValue> updates, Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = updates.Select(u => new { u.Id, OrderKey = ResolveOrderKey(u.OrderKey, u.PreviousItemOrderKey, u.NextItemOrderKey) })
                          .Where(x => x.OrderKey is not null)
                          .ToArray();
        if (rows.Length == 0) return;

        await connection.ExecuteAsync(new CommandDefinition(
            @"UPDATE project_folders f SET order_key = v.order_key, updated_at = NOW()
              FROM UNNEST(@Ids, @OrderKeys) AS v(id, order_key)
              WHERE f.id = v.id AND f.project_workspace_id = @WorkspaceId",
            new { Ids = rows.Select(r => r.Id).ToArray(), OrderKeys = rows.Select(r => r.OrderKey).ToArray(), WorkspaceId = workspaceId },
            cancellationToken: cancellationToken));
    }

    private static async Task ApplyTaskStatusBatch(DbConnection connection, List<BatchUpdateSpaceItemValue> updates, Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = updates.Where(u => u.StatusId.HasValue).ToArray();
        if (rows.Length == 0) return;

        await connection.ExecuteAsync(new CommandDefinition(
            @"UPDATE project_tasks t
              SET status_id = CASE WHEN v.status_id = '00000000-0000-0000-0000-000000000000' THEN NULL ELSE v.status_id END, updated_at = NOW()
              FROM UNNEST(@Ids, @StatusIds) AS v(id, status_id)
              WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
            new { Ids = rows.Select(r => r.Id).ToArray(), StatusIds = rows.Select(r => r.StatusId!.Value).ToArray(), WorkspaceId = workspaceId },
            cancellationToken: cancellationToken));
    }

    private static async Task ApplyFolderStatusBatch(DbConnection connection, List<BatchUpdateSpaceItemValue> updates, Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = updates.Where(u => u.StatusId.HasValue).ToArray();
        if (rows.Length == 0) return;

        await connection.ExecuteAsync(new CommandDefinition(
            @"UPDATE project_folders f
              SET status_id = CASE WHEN v.status_id = '00000000-0000-0000-0000-000000000000' THEN NULL ELSE v.status_id END, updated_at = NOW()
              FROM UNNEST(@Ids, @StatusIds) AS v(id, status_id)
              WHERE f.id = v.id AND f.project_workspace_id = @WorkspaceId",
            new { Ids = rows.Select(r => r.Id).ToArray(), StatusIds = rows.Select(r => r.StatusId!.Value).ToArray(), WorkspaceId = workspaceId },
            cancellationToken: cancellationToken));
    }

    private static async Task ApplyTaskPriorityBatch(DbConnection connection, List<BatchUpdateSpaceItemValue> updates, Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = updates.Where(u => u.Priority is not null).ToArray();
        if (rows.Length == 0) return;

        await connection.ExecuteAsync(new CommandDefinition(
            @"UPDATE project_tasks t SET priority = v.priority, updated_at = NOW()
              FROM UNNEST(@Ids, @Priorities) AS v(id, priority)
              WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
            new { Ids = rows.Select(r => r.Id).ToArray(), Priorities = rows.Select(r => (int)r.Priority!.Value).ToArray(), WorkspaceId = workspaceId },
            cancellationToken: cancellationToken));
    }

    private static async Task ApplyFolderPriorityBatch(DbConnection connection, List<BatchUpdateSpaceItemValue> updates, Guid workspaceId, CancellationToken cancellationToken)
    {
        var rows = updates.Where(u => u.Priority is not null).ToArray();
        if (rows.Length == 0) return;

        await connection.ExecuteAsync(new CommandDefinition(
            @"UPDATE project_folders f SET priority = v.priority, updated_at = NOW()
              FROM UNNEST(@Ids, @Priorities) AS v(id, priority)
              WHERE f.id = v.id AND f.project_workspace_id = @WorkspaceId",
            new { Ids = rows.Select(r => r.Id).ToArray(), Priorities = rows.Select(r => (int)r.Priority!.Value).ToArray(), WorkspaceId = workspaceId },
            cancellationToken: cancellationToken));
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