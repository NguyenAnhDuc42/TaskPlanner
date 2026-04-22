using Background.Jobs;
using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Background.Services;

public class HangfireOutboxTrigger : IOutboxTrigger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HangfireOutboxTrigger> _logger;

    private static long _lastTriggerTicks;
    private static readonly long DebounceIntervalTicks = TimeSpan.FromSeconds(2).Ticks;

    public HangfireOutboxTrigger(IServiceScopeFactory scopeFactory, ILogger<HangfireOutboxTrigger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Trigger()
    {
        var now = DateTimeOffset.UtcNow.Ticks;
        var last = Interlocked.Read(ref _lastTriggerTicks);

        if (now - last < DebounceIntervalTicks) return;

        if (Interlocked.CompareExchange(ref _lastTriggerTicks, now, last) == last)
        {
            // Process outbox directly on a background thread — no queue delay
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var job = scope.ServiceProvider.GetRequiredService<ProcessOutboxJob>();
                    await job.RunAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Direct outbox processing failed — recurring safety net will retry");
                }
            });
        }
    }
}
