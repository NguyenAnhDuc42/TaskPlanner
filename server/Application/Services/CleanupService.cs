using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application;

public class CleanupService(IServiceScopeFactory scopeFactory, ILogger<CleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay initial run by 5 minutes so the app fully starts first
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Cleanup] Failed during cleanup run");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskPlanDbContext>();
        var conn = db.Database.GetDbConnection();

        var cutoff = DateTimeOffset.UtcNow - RetentionPeriod;

        var tables = new[]
        {
            "entity_access",
            "workspace_members",
            "project_spaces",
            "project_folders",
            "project_tasks",
            "comments",
            "favorites",
            "statuses",
            "documents",
            "document_blocks",
            "notifications",
        };

        var totalDeleted = 0;
        foreach (var table in tables)
        {
            try
            {
                var deleted = await conn.ExecuteAsync(
                    $"DELETE FROM {table} WHERE deleted_at IS NOT NULL AND deleted_at < @Cutoff",
                    new { Cutoff = cutoff });
                if (deleted > 0)
                {
                    logger.LogInformation("[Cleanup] Hard-deleted {Count} rows from {Table}", deleted, table);
                    totalDeleted += deleted;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Cleanup] Failed to clean table {Table}", table);
            }
        }

        if (totalDeleted > 0)
            logger.LogInformation("[Cleanup] Run complete — {Total} rows purged", totalDeleted);
    }
}
