using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Application;

public class BatchMoveItemHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    RealtimeService realtime
) : ICommandHandler<BatchMoveItemCommand>
{
    public async Task<Result> Handle(BatchMoveItemCommand request, CancellationToken cancellationToken)
    {
        if (!request.HasAnyMoves)
            return Result.Success();

        if (context.CurrentMember.Role != Role.Admin && context.CurrentMember.Role != Role.Owner)
            return Result.Failure(MemberError.DontHavePermission);

        if (request.Folders.Count > 0)
        {
            var folderValidation = await new BatchMoveValidator(db, context.workspaceId)
                .ValidateFolderMovesAsync(request.Folders, cancellationToken);
            if (folderValidation.IsFailure) return folderValidation;
        }

        if (request.Tasks.Count > 0)
        {
            var taskValidation = await new BatchMoveValidator(db, context.workspaceId)
                .ValidateTaskMovesAsync(request.Tasks, cancellationToken);
            if (taskValidation.IsFailure) return taskValidation;
        }

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            await ApplyMovesAsync(request, cancellationToken);
            return Result.Success();
        }, cancellationToken);

        if (result.IsFailure)
            return result;

        db.ChangeTracker.Clear();

        var packet = await FetchUpdatedRecordsAsync(request, cancellationToken);

        if (packet.HasAny)
            await realtime.NotifyEntitiesUpdatedAsync(context.workspaceId, packet, cancellationToken);

        return Result.Success();
    }


    private async Task ApplyMovesAsync(BatchMoveItemCommand request, CancellationToken cancellationToken)
    {
        if (request.Spaces.Count > 0)
        {
            var ids = request.Spaces.Select(s => s.ItemId).ToArray();
            var orderKeys = request.Spaces.Select(s => s.NewOrderKey).ToArray();

            await db.Database.ExecuteSqlRawAsync(
                """
                UPDATE project_spaces s
                SET order_key = v.order_key,
                    updated_at = NOW()
                FROM UNNEST(@ids, @orderKeys) AS v(id, order_key)
                WHERE s.id = v.id AND s.project_workspace_id = @workspaceId
                """,
                parameters: new object[]
                {
                    new NpgsqlParameter("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = ids },
                    new NpgsqlParameter("orderKeys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = orderKeys },
                    new NpgsqlParameter("workspaceId", context.workspaceId)
                },
                cancellationToken: cancellationToken
            );
        }

        if (request.Folders.Count > 0)
        {
            var withParentChange = request.Folders.Where(f => f.TargetParentId.HasValue).ToList();
            var reorderOnly      = request.Folders.Where(f => !f.TargetParentId.HasValue).ToList();

            if (withParentChange.Count > 0)
            {
                foreach (var group in withParentChange.GroupBy(f => f.TargetParentId!.Value))
                {
                    var ids = group.Select(f => f.ItemId).ToArray();
                    var orderKeys = group.Select(f => f.NewOrderKey).ToArray();
                    var spaceId = group.Key;

                    await db.Database.ExecuteSqlRawAsync(
                        """
                        UPDATE project_folders f
                        SET project_space_id = @spaceId,
                            order_key = v.order_key,
                            updated_at = NOW()
                        FROM UNNEST(@ids, @orderKeys) AS v(id, order_key)
                        WHERE f.id = v.id AND f.project_workspace_id = @workspaceId
                        """,
                        parameters: new object[]
                        {
                            new NpgsqlParameter("spaceId", spaceId),
                            new NpgsqlParameter("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = ids },
                            new NpgsqlParameter("orderKeys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = orderKeys },
                            new NpgsqlParameter("workspaceId", context.workspaceId)
                        },
                        cancellationToken: cancellationToken
                    );
                }
            }

            if (reorderOnly.Count > 0)
            {
                var ids = reorderOnly.Select(f => f.ItemId).ToArray();
                var orderKeys = reorderOnly.Select(f => f.NewOrderKey).ToArray();

                await db.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE project_folders f
                    SET order_key = v.order_key,
                        updated_at = NOW()
                    FROM UNNEST(@ids, @orderKeys) AS v(id, order_key)
                    WHERE f.id = v.id AND f.project_workspace_id = @workspaceId
                    """,
                    parameters: new object[]
                    {
                        new NpgsqlParameter("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = ids },
                        new NpgsqlParameter("orderKeys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = orderKeys },
                        new NpgsqlParameter("workspaceId", context.workspaceId)
                    },
                    cancellationToken: cancellationToken
                );
            }
        }

        if (request.Tasks.Count > 0)
        {
            foreach (var group in request.Tasks.GroupBy(t => (t.TargetSpaceId, t.TargetFolderId)))
            {
                var ids = group.Select(t => t.ItemId).ToArray();
                var orderKeys = group.Select(t => t.NewOrderKey).ToArray();
                var spaceId = group.Key.TargetSpaceId;
                var folderId = group.Key.TargetFolderId;

                await db.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE project_tasks t
                    SET project_space_id = @spaceId,
                        project_folder_id = @folderId,
                        order_key = v.order_key,
                        updated_at = NOW()
                    FROM UNNEST(@ids, @orderKeys) AS v(id, order_key)
                    WHERE t.id = v.id AND t.project_workspace_id = @workspaceId
                    """,
                    parameters: new object[]
                    {
                        new NpgsqlParameter("spaceId", spaceId),
                        new NpgsqlParameter("folderId", (object?)folderId ?? DBNull.Value),
                        new NpgsqlParameter("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = ids },
                        new NpgsqlParameter("orderKeys", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = orderKeys },
                        new NpgsqlParameter("workspaceId", context.workspaceId)
                    },
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    private async Task<EntityBatchUpdate> FetchUpdatedRecordsAsync(BatchMoveItemCommand request, CancellationToken cancellationToken)
    {
        var spaces = request.Spaces.Count > 0
            ? await FetchSpacesAsync(request.Spaces.Select(s => s.ItemId).ToList(), cancellationToken)
            : [];

        var folders = request.Folders.Count > 0
            ? await FetchFoldersAsync(request.Folders.Select(f => f.ItemId).ToList(), cancellationToken)
            : [];

        var tasks = request.Tasks.Count > 0
            ? await FetchTasksAsync(request.Tasks.Select(t => t.ItemId).ToList(), cancellationToken)
            : [];

        return new EntityBatchUpdate
        {
            Spaces  = spaces.NullIfEmpty(),
            Folders = folders.NullIfEmpty(),
            Tasks   = tasks.NullIfEmpty()
        };
    }

    private Task<List<SpaceRecord>> FetchSpacesAsync(List<Guid> ids, CancellationToken cancellationToken) =>
        db.ProjectSpaces
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new SpaceRecord
            {
                Id          = s.Id,
                WorkspaceId = s.ProjectWorkspaceId,
                Name        = s.Name,
                Color       = s.Color,
                Icon        = s.Icon,
                IsPrivate   = s.IsPrivate,
                OrderKey    = s.OrderKey
            })
            .ToListAsync(cancellationToken);

    private Task<List<FolderRecord>> FetchFoldersAsync(List<Guid> ids, CancellationToken cancellationToken) =>
        db.ProjectFolders
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .Select(f => new FolderRecord
            {
                Id          = f.Id,
                WorkspaceId = context.workspaceId,
                SpaceId     = f.ProjectSpaceId,
                Name        = f.Name,
                OrderKey    = f.OrderKey,
                Icon        = f.Icon,
                Color       = f.Color
            })
            .ToListAsync(cancellationToken);

    private Task<List<TaskRecord>> FetchTasksAsync(List<Guid> ids, CancellationToken cancellationToken) =>
         db.ProjectTasks
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .Select(t => new TaskRecord
            {
                Id              = t.Id,
                WorkspaceId     = context.workspaceId,
                Name            = t.Name,
                StatusId        = t.StatusId,
                Priority        = t.Priority,
                StartDate       = t.StartDate,
                DueDate         = t.DueDate,
                OrderKey        = t.OrderKey,
                Icon            = t.Icon,
                Color           = t.Color,
                ProjectSpaceId  = t.ProjectSpaceId,
                ProjectFolderId = t.ProjectFolderId
            })
            .ToListAsync(cancellationToken);
}

public static class ListExtensions
{
    public static List<T>? NullIfEmpty<T>(this List<T> list) =>
        list.Count > 0 ? list : null;
}
