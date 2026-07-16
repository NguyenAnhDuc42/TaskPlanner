using Dapper;

namespace Api;

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

        var targets = db.Model.GetEntityTypes()
            .Select(et => new { Table = et.GetTableName(), DeletedAt = et.FindProperty("DeletedAt") })
            .Where(t => t.Table != null && t.DeletedAt != null)
            .Select(t => new { Table = t.Table!, Column = t.DeletedAt!.GetColumnName() })
            .Distinct()
            .ToList();

        var totalDeleted = 0;
        foreach (var target in targets)
        {
            try
            {
                var deleted = await conn.ExecuteAsync(
                    $"DELETE FROM {target.Table} WHERE {target.Column} IS NOT NULL AND {target.Column} < @Cutoff",
                    new { Cutoff = cutoff });
                if (deleted > 0)
                {
                    logger.LogInformation("[Cleanup] Hard-deleted {Count} rows from {Table}", deleted, target.Table);
                    totalDeleted += deleted;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Cleanup] Failed to clean table {Table}", target.Table);
            }
        }

        if (totalDeleted > 0)
            logger.LogInformation("[Cleanup] Run complete — {Total} rows purged", totalDeleted);
    }
}
