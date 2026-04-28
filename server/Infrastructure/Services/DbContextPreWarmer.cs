using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Background service that pre-warms the EF Core DbContext on startup.
/// This prevents the 500ms+ delay on the first request by forcing EF to compile its model early.
/// </summary>
public class DbContextPreWarmer(
    IServiceScopeFactory scopeFactory,
    ILogger<DbContextPreWarmer> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("EF Core Pre-warmer: Starting model compilation in background...");
            
            // We use a separate thread so we don't block the app startup
            await Task.Run(() =>
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TaskPlanDbContext>();
                
                // Accessing the Model property forces EF to compile the entire model in-memory
                _ = db.Model;
                
                logger.LogInformation("EF Core Pre-warmer: Model compilation complete.");
            }, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EF Core Pre-warmer: Failed to pre-warm DbContext. This won't break the app, but the first request might be slower.");
        }
    }
}
