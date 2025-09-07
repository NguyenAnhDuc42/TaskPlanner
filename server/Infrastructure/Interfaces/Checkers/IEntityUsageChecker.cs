using System;

namespace Infrastructure.Interfaces.Checkers;

public interface IEntityUsageChecker
{
    Task<bool> IsInUseAsync(Guid entityId, CancellationToken ct = default);
    Task<int> GetUsageCountAsync(Guid entityId, CancellationToken ct = default);
    Task<IEnumerable<Guid>> GetUsageSampleIdsAsync(Guid entityId, int resultCount = 5, CancellationToken ct = default);
}
