using Microsoft.EntityFrameworkCore;

namespace Api;


public class IdempotencyService(TaskPlanDbContext dbContext)
{
    private HashSet<string>? _processedCache;

    public async Task PreloadAsync(IEnumerable<string> traceIds, CancellationToken cancellationToken = default)
    {
        var ids = traceIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToArray();
        _processedCache = ids.Length > 0
            ? (await dbContext.ProcessedTraces
                .Where(t => ids.Contains(t.TraceId))
                .Select(t => t.TraceId)
                .ToListAsync(cancellationToken)).ToHashSet()
            : [];
    }

    public async Task<bool> HasProcessedAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(traceId)) return false;

        if (_processedCache != null) return _processedCache.Contains(traceId);

        return await dbContext.ProcessedTraces
            .AnyAsync(t => t.TraceId == traceId, cancellationToken);
    }

    public void MarkAsProcessed(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId)) return;

        dbContext.ProcessedTraces.Add(new ProcessedTrace { TraceId = traceId });
        
        _processedCache?.Add(traceId);
    }
}
