using Microsoft.EntityFrameworkCore;

namespace Api;

public class IdempotencyService(TaskPlanDbContext dbContext)
{
    public async Task<bool> HasProcessedAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId)) return false;

        return await dbContext.ProcessedTraces
            .AnyAsync(t => t.TraceId == traceId, cancellationToken);
    }

    public void MarkAsProcessed(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId)) return;

        dbContext.ProcessedTraces.Add(new ProcessedTrace { TraceId = traceId });
    }
}
