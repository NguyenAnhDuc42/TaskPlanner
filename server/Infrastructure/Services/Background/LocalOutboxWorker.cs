using System.Threading.Channels;
using Background.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Background;

public class LocalOutboxWorker : BackgroundService
{
    private readonly Channel<bool> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LocalOutboxWorker> _logger;

    public LocalOutboxWorker(Channel<bool> channel, IServiceProvider serviceProvider, ILogger<LocalOutboxWorker> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Local Outbox Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _channel.Reader.ReadAsync(stoppingToken);
                await Task.Delay(100, stoppingToken);

                _logger.LogTrace("Local Outbox Worker triggered.");

                bool hasMore;
                do
                {
                    using var scope = _serviceProvider.CreateScope();
                    var job = scope.ServiceProvider.GetRequiredService<ProcessOutboxJob>();
                    hasMore = await job.RunAsync();
                } while (hasMore && !stoppingToken.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Local Outbox Worker.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
