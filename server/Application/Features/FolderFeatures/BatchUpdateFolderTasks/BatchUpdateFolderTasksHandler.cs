using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Application;

public class BatchUpdateFolderTasksHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService)
    : ICommandHandler<BatchUpdateFolderTasksCommand>
{
    public async Task<Result> Handle(BatchUpdateFolderTasksCommand request, CancellationToken cancellationToken)
    {
        var folder = await db.ProjectFolders
            .AsNoTracking()
            .Where(f => f.Id == request.FolderId && f.DeletedAt == null)
            .Select(f => new { f.CreatorId, f.ProjectSpaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (folder == null) return Result.Failure(FolderError.NotFound);
        var isCreator = folder.CreatorId == workspaceContext.CurrentMember.Id;
        if (!isCreator)
        {
            var hasAccess = await permissionService.VerifyAsync(Role.Member, folder.ProjectSpaceId, AccessLevel.Editor, cancellationToken);
            if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        }


        if (request.Updates is not { Count: > 0 }) return Result.Success();

        var result = await db.ExecuteInTransactionAsync(
            () => ApplyBatchAsync(request, cancellationToken), cancellationToken);

        if (result.IsSuccess)
        {
            var updatedIds = request.Updates.Where(u => u.IsDeleted != true).Select(u => u.Id).ToList();
            var deletedIds = request.Updates.Where(u => u.IsDeleted == true).Select(u => u.Id).ToList();
            if (updatedIds.Count > 0)
            {
                var tasks = (await db.ProjectTasks
                    .AsNoTracking()
                    .Where(t => updatedIds.Contains(t.Id))
                    .ToListAsync(cancellationToken))
                    .Select(TaskRecord.FromDomain)
                    .ToList();

                await realtimeService.NotifyEntitiesUpdatedAsync(
                    workspaceContext.WorkspaceId,
                    new EntityBatchUpdate { Tasks = tasks },
                    cancellationToken);
            }
            if (deletedIds.Count > 0)
            {
                await realtimeService.NotifyEntitiesDeletedAsync(
                    workspaceContext.WorkspaceId,
                    new EntityBatchDelete { TaskIds = deletedIds },
                    cancellationToken);
            }
        }
        return result;
    }

    private async Task<Result> ApplyBatchAsync(BatchUpdateFolderTasksCommand request, CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        var workspaceId = workspaceContext.WorkspaceId;

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

        await ApplyOrderKeyBatch(connection, request, workspaceId, cancellationToken);
        await ApplyStatusBatch(connection, request, workspaceId, cancellationToken);
        await ApplyPriorityBatch(connection, request, workspaceId, cancellationToken);
        await ApplyDatesBatch(connection, request, workspaceId, cancellationToken);
        await ApplySoftDeleteBatch(connection, request, workspaceId, cancellationToken);

        return Result.Success();
    }

    private static async Task ApplyOrderKeyBatch(
        DbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var rows = request.Updates
            .Where(u => u.OrderKey is not null)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
           new CommandDefinition(
                @"UPDATE project_tasks t
                SET order_key = v.order_key,
                    updated_at = NOW()
                FROM UNNEST(@Ids, @OrderKeys) AS v(id, order_key)
                WHERE t.id = v.id
                    AND t.project_workspace_id = @WorkspaceId",
            new
            {
                Ids = rows.Select(r => r.Id).ToArray(),
                OrderKeys = rows.Select(r => r.OrderKey).ToArray(),
                WorkspaceId = workspaceId
            }, cancellationToken: cancellationToken));
    }

    private static async Task ApplyStatusBatch(
        DbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var rows = request.Updates
            .Where(u => u.StatusId.HasValue)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
            new CommandDefinition(
            @"UPDATE project_tasks t
              SET status_id = CASE WHEN v.status_id = '00000000-0000-0000-0000-000000000000' 
                                   THEN NULL ELSE v.status_id END,
                  updated_at = NOW()
              FROM UNNEST(@Ids, @StatusIds) AS v(id, status_id)
              WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
            new
            {
                Ids = rows.Select(r => r.Id).ToArray(),
                StatusIds = rows.Select(r => r.StatusId!.Value).ToArray(),
                WorkspaceId = workspaceId
            }, cancellationToken: cancellationToken));
    }

    private static async Task ApplyPriorityBatch(
        DbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var rows = request.Updates
            .Where(u => u.Priority is not null)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
            new CommandDefinition(
                @"UPDATE project_tasks t
                  SET priority = v.priority, updated_at = NOW()
                  FROM UNNEST(@Ids, @Priorities) AS v(id, priority)
                  WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
                new
                {
                    Ids = rows.Select(r => r.Id).ToArray(),
                    Priorities = rows.Select(r => (int)r.Priority!).ToArray(),
                    WorkspaceId = workspaceId
                }, cancellationToken: cancellationToken));
    }

    private static async Task ApplyDatesBatch(
        DbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var startRows = request.Updates.Where(u => u.StartDate is not null).ToArray();
        var dueRows = request.Updates.Where(u => u.DueDate is not null).ToArray();

        if (startRows.Length > 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                @"UPDATE project_tasks t
                  SET start_date = v.start_date, updated_at = NOW()
                  FROM UNNEST(@Ids, @Dates) AS v(id, start_date)
                  WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
                new
                {
                    Ids = startRows.Select(r => r.Id).ToArray(),
                    Dates = startRows.Select(r => r.StartDate!.Value).ToArray(),
                    WorkspaceId = workspaceId
                }, cancellationToken: cancellationToken));
        }

        if (dueRows.Length > 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    @"UPDATE project_tasks t
                      SET due_date = v.due_date, updated_at = NOW()
                      FROM UNNEST(@Ids, @Dates) AS v(id, due_date)
                      WHERE t.id = v.id AND t.project_workspace_id = @WorkspaceId",
                    new
                    {
                    Ids = dueRows.Select(r => r.Id).ToArray(),
                    Dates = dueRows.Select(r => r.DueDate!.Value).ToArray(),
                    WorkspaceId = workspaceId
                }, cancellationToken: cancellationToken));
        }
    }

    private static async Task ApplySoftDeleteBatch(
        DbConnection connection,
        BatchUpdateFolderTasksCommand request,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var rows = request.Updates
            .Where(u => u.IsDeleted is true)
            .ToArray();

        if (rows.Length == 0) return;

        await connection.ExecuteAsync(
            new CommandDefinition(
                @"UPDATE project_tasks
                  SET deleted_at = NOW(), updated_at = NOW()
                  WHERE id = ANY(@Ids) AND project_workspace_id = @WorkspaceId",
                new
                {
                    Ids = rows.Select(r => r.Id).ToArray(),
                    WorkspaceId = workspaceId
                }, cancellationToken: cancellationToken));
    }
}