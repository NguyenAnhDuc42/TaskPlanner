using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchUpdateFolderTasksHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    RealtimeService realtimeService)
    : ICommandHandler<BatchUpdateFolderTasksCommand>
{
    public async Task<Result> Handle(BatchUpdateFolderTasksCommand request, CancellationToken ct)
    {
        if (request.Updates is not { Count: > 0 })
            return Result.Success();

        var result = await db.ExecuteInTransactionAsync(
            () => ApplyBatchAsync(request, ct), ct);

        if (result.IsSuccess)
        {
            await realtimeService.NotifyWorkspaceAsync(
                workspaceContext.workspaceId,
                "FolderTasksBatchUpdated",
                new { FolderId = request.FolderId },
                ct);
        }

        return result;
    }

    private async Task<Result> ApplyBatchAsync(BatchUpdateFolderTasksCommand request, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var workspaceId = workspaceContext.workspaceId;

        // Validate all IDs exist in this folder first — one round trip
        var requestedIds = request.Updates.Select(u => u.Id).ToArray();

        var existingIds = (await connection.QueryAsync<Guid>(
            @"SELECT id FROM project_tasks 
              WHERE id = ANY(@Ids) 
                AND project_folder_id = @FolderId 
                AND project_workspace_id = @WorkspaceId
                AND deleted_at IS NULL",
            new { Ids = requestedIds, FolderId = request.FolderId, WorkspaceId = workspaceId }
        )).ToHashSet();

        var missing = requestedIds.FirstOrDefault(id => !existingIds.Contains(id));
        if (missing != default)
            return Result.Failure(Error.NotFound("Task.NotFound", $"Task {missing} not found in folder {request.FolderId}"));

        // One UPDATE per distinct update shape, batched with UNNEST
        await ApplyOrderKeyBatch(connection, request, workspaceId);
        await ApplyStatusBatch(connection, request, workspaceId);
        await ApplyPriorityBatch(connection, request, workspaceId);
        await ApplyDatesBatch(connection, request, workspaceId);
        await ApplySoftDeleteBatch(connection, request, workspaceId);

        return Result.Success();
    }

    private static async Task ApplyOrderKeyBatch(
        System.Data.IDbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId)
    {
        var rows = request.Updates
            .Where(u => u.OrderKey is not null)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
            @"UPDATE project_tasks t
              SET order_key = v.order_key, updated_at = NOW()
              FROM UNNEST(@Ids, @OrderKeys) AS v(id uuid, order_key text)
              WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
            new
            {
                Ids = rows.Select(r => r.Id).ToArray(),
                OrderKeys = rows.Select(r => r.OrderKey).ToArray(),
                WorkspaceId = workspaceId
            });
    }

    private static async Task ApplyStatusBatch(
        System.Data.IDbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId)
    {
        var rows = request.Updates
            .Where(u => u.StatusId.HasValue)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
            @"UPDATE project_tasks t
              SET status_id = CASE WHEN v.status_id = '00000000-0000-0000-0000-000000000000' 
                                   THEN NULL ELSE v.status_id END,
                  updated_at = NOW()
              FROM UNNEST(@Ids, @StatusIds) AS v(id uuid, status_id uuid)
              WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
            new
            {
                Ids = rows.Select(r => r.Id).ToArray(),
                StatusIds = rows.Select(r => r.StatusId!.Value).ToArray(),
                WorkspaceId = workspaceId
            });
    }

    private static async Task ApplyPriorityBatch(
        System.Data.IDbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId)
    {
        var rows = request.Updates
            .Where(u => u.Priority is not null)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
            @"UPDATE project_tasks t
              SET priority = v.priority, updated_at = NOW()
              FROM UNNEST(@Ids, @Priorities) AS v(id uuid, priority int)
              WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
            new
            {
                Ids = rows.Select(r => r.Id).ToArray(),
                Priorities = rows.Select(r => (int)r.Priority!).ToArray(),
                WorkspaceId = workspaceId
            });
    }

    private static async Task ApplyDatesBatch(
        System.Data.IDbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId)
    {
        var startRows = request.Updates.Where(u => u.StartDate is not null).ToArray();
        var dueRows = request.Updates.Where(u => u.DueDate is not null).ToArray();

        if (startRows.Length > 0)
        {
            await connection.ExecuteAsync(
                @"UPDATE project_tasks t
                  SET start_date = v.start_date, updated_at = NOW()
                  FROM UNNEST(@Ids, @Dates) AS v(id uuid, start_date timestamptz)
                  WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
                new
                {
                    Ids = startRows.Select(r => r.Id).ToArray(),
                    Dates = startRows.Select(r => r.StartDate!.Value).ToArray(),
                    WorkspaceId = workspaceId
                });
        }

        if (dueRows.Length > 0)
        {
            await connection.ExecuteAsync(
                @"UPDATE project_tasks t
                  SET due_date = v.due_date, updated_at = NOW()
                  FROM UNNEST(@Ids, @Dates) AS v(id uuid, due_date timestamptz)
                  WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
                new
                {
                    Ids = dueRows.Select(r => r.Id).ToArray(),
                    Dates = dueRows.Select(r => r.DueDate!.Value).ToArray(),
                    WorkspaceId = workspaceId
                });
        }
    }

    private static async Task ApplySoftDeleteBatch(
        System.Data.IDbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId)
    {
        var rows = request.Updates
            .Where(u => u.IsDeleted is true)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
            @"UPDATE project_tasks
              SET deleted_at = NOW(), updated_at = NOW()
              WHERE id = ANY(@Ids) AND project_workspace_id = @WorkspaceId",
            new
            {
                Ids = rows.Select(r => r.Id).ToArray(),
                WorkspaceId = workspaceId
            });
    }
}