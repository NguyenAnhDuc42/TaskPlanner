using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.Widget;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Features.DashboardFeatures.WidgetDataHelper;



public class WidgetBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WidgetBuilder> _logger;

    public WidgetBuilder(IServiceProvider serviceProvider, ILogger<WidgetBuilder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task BuildAndNotifyAsync(IEnumerable<Widget> widgets, Guid userId, CancellationToken ct)
    {
        _logger.LogInformation("Starting background data build for {Count} widgets. UserId: {UserId}", widgets.Count(), userId);

        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var realtime = scope.ServiceProvider.GetRequiredService<IRealtimeService>();

            foreach (var widget in widgets)
            {
                try
                {
                    _logger.LogDebug("Building data for Widget: {WidgetId} (Type: {Type})", widget.Id, widget.WidgetType);
                    
                    var widgetData = await BuildDataItemAsync(unitOfWork, widget, ct);
                    await realtime.NotifyDashboardAsync(widget.DashboardId, "WidgetDataLoaded", widgetData, ct);
                    _logger.LogInformation("Successfully pushed data for Widget: {WidgetId} ({State}) to Dashboard Group: {DashboardId}", 
                        widget.Id, widgetData.GetType().Name, widget.DashboardId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error building/pushing data for Widget: {WidgetId}", widget.Id);
                    await realtime.NotifyDashboardAsync(widget.DashboardId, "WidgetDataError", new 
                    { 
                        WidgetId = widget.Id, 
                        Error = "Failed to build widget data." 
                    }, ct);
                }
            }
            
            _logger.LogInformation("Completed background data build for UserId: {UserId}", userId);
        }, ct);

        await Task.CompletedTask;
    }

    private async Task<WidgetData> BuildDataItemAsync(IUnitOfWork unitOfWork, Widget widget, CancellationToken ct)
    {
        var position = new WidgetPosition(widget.Layout.Col, widget.Layout.Row, widget.Layout.Width, widget.Layout.Height);
        
        switch (widget.WidgetType)
        {
            case WidgetType.TaskList:
                var tasks = await TaskListWidgetSQL.ExecuteAsync(unitOfWork, widget.LayerId, widget.LayerType, widget.ConfigJson, ct);
                var now = DateTime.UtcNow.Date;
                return new TaskStatusWidgetData
                {
                    WidgetId = widget.Id,
                    Position = position,
                    Tasks = tasks,
                    TotalCount = tasks.Count,
                    OverdueCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.UtcDateTime.Date < now),
                    TodayCount = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value.UtcDateTime.Date == now)
                };

            case WidgetType.FolderList:
                return new FolderListWidgetData { WidgetId = widget.Id, Position = position };

            case WidgetType.ActivityFeed:
                return new ActivityFeedWidgetData { WidgetId = widget.Id, Position = position };

            default:
                return new HealthCheckWidgetData { WidgetId = widget.Id, Position = position };
        }
    }
}
