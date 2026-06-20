using Dapper;
using Domain;
using Microsoft.EntityFrameworkCore;
using Application;

namespace Api;

public static class OrderKeyMigration
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskPlanDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var conn = db.Database.GetDbConnection();

        logger.LogInformation("OrderKeyMigration: checking for legacy order keys...");

        var spacesFixed   = await ResetTableAsync(conn, "project_spaces",  "project_workspace_id", logger);
        var foldersFixed  = await ResetTableAsync(conn, "project_folders", "project_space_id",     logger);
        var tasksFixed    = await ResetTasksAsync(conn, logger);
        var statusesFixed = await ResetStatusesAsync(conn, logger);

        if (spacesFixed + foldersFixed + tasksFixed + statusesFixed == 0)
            logger.LogInformation("OrderKeyMigration: all order keys are valid, nothing to fix.");
        else
            logger.LogInformation("OrderKeyMigration: fixed {Total} rows across spaces/folders/tasks/statuses.",
                spacesFixed + foldersFixed + tasksFixed + statusesFixed);
    }

    private static async Task<int> ResetTableAsync(
        System.Data.Common.DbConnection conn,
        string table,
        string groupCol,
        ILogger logger)
    {
        var rows = (await conn.QueryAsync<(Guid Id, Guid GroupId, string? OrderKey)>(
            $"SELECT id, {groupCol}, order_key FROM {table} WHERE deleted_at IS NULL ORDER BY {groupCol}, order_key, id")).AsList();

        var bad = rows.Where(r => !FractionalIndex.IsValid(r.OrderKey)).ToList();
        if (bad.Count == 0) return 0;

        logger.LogWarning("OrderKeyMigration: {Table} has {Count} rows with invalid order keys — resetting all keys per group.", table, bad.Count);

        var ids = new List<Guid>();
        var keys = new List<string>();

        foreach (var group in rows.GroupBy(r => r.GroupId))
        {
            var list = group.ToList();
            var generated = GenerateKeys(list.Count);
            for (var i = 0; i < list.Count; i++) { ids.Add(list[i].Id); keys.Add(generated[i]); }
        }

        await conn.ExecuteAsync(
            $"UPDATE {table} SET order_key = v.key, updated_at = NOW() FROM UNNEST(@ids, @keys) AS v(id, key) WHERE {table}.id = v.id",
            new { ids = ids.ToArray(), keys = keys.ToArray() });

        return ids.Count;
    }

    private static async Task<int> ResetTasksAsync(System.Data.Common.DbConnection conn, ILogger logger)
    {
        var rows = (await conn.QueryAsync<(Guid Id, Guid SpaceId, Guid? FolderId, string? OrderKey)>(
            "SELECT id, project_space_id, project_folder_id, order_key FROM project_tasks WHERE deleted_at IS NULL ORDER BY project_space_id, project_folder_id, order_key, id")).AsList();

        var bad = rows.Where(r => !FractionalIndex.IsValid(r.OrderKey)).ToList();
        if (bad.Count == 0) return 0;

        logger.LogWarning("OrderKeyMigration: project_tasks has {Count} rows with invalid order keys — resetting.", bad.Count);

        var ids = new List<Guid>();
        var keys = new List<string>();

        foreach (var group in rows.GroupBy(r => (r.SpaceId, r.FolderId)))
        {
            var list = group.ToList();
            var generated = GenerateKeys(list.Count);
            for (var i = 0; i < list.Count; i++) { ids.Add(list[i].Id); keys.Add(generated[i]); }
        }

        await conn.ExecuteAsync(
            "UPDATE project_tasks SET order_key = v.key, updated_at = NOW() FROM UNNEST(@ids, @keys) AS v(id, key) WHERE project_tasks.id = v.id",
            new { ids = ids.ToArray(), keys = keys.ToArray() });

        return ids.Count;
    }

    private static async Task<int> ResetStatusesAsync(System.Data.Common.DbConnection conn, ILogger logger)
    {
        var rows = (await conn.QueryAsync<(Guid Id, Guid WorkflowId, string? OrderKey)>(
            "SELECT id, workflow_id, order_key FROM statuses WHERE deleted_at IS NULL ORDER BY workflow_id, order_key, id")).AsList();

        var bad = rows.Where(r => !FractionalIndex.IsValid(r.OrderKey)).ToList();
        if (bad.Count == 0) return 0;

        logger.LogWarning("OrderKeyMigration: statuses has {Count} rows with invalid order keys — resetting.", bad.Count);

        var ids = new List<Guid>();
        var keys = new List<string>();

        foreach (var group in rows.GroupBy(r => r.WorkflowId))
        {
            var list = group.ToList();
            var generated = GenerateKeys(list.Count);
            for (var i = 0; i < list.Count; i++) { ids.Add(list[i].Id); keys.Add(generated[i]); }
        }

        await conn.ExecuteAsync(
            "UPDATE statuses SET order_key = v.key, updated_at = NOW() FROM UNNEST(@ids, @keys) AS v(id, key) WHERE statuses.id = v.id",
            new { ids = ids.ToArray(), keys = keys.ToArray() });

        return ids.Count;
    }

    private static List<string> GenerateKeys(int count)
    {
        var result = new List<string>(count);
        string? prev = null;
        for (var i = 0; i < count; i++)
        {
            var key = prev is null ? FractionalIndex.Start() : FractionalIndex.After(prev);
            result.Add(key);
            prev = key;
        }
        return result;
    }
}
