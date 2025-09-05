using System;

namespace Domain.Services.UsageChecker;

public interface IEntityUsageChecker
{
    Task<bool> IsInUseAsync(Guid entityId, CancellationToken ct = default);
    Task<int> GetUsageCountAsync(Guid entityId, CancellationToken ct = default);
    Task<IEnumerable<Guid>> GetUsageSampleIdsAsync(Guid entityId, int resultCount = 5, CancellationToken ct = default);
}