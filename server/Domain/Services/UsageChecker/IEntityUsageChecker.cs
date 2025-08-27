using System;

namespace Domain.Services.UsageChecker;

 public interface IEntityUsageChecker
    {
        /// <summary>
        /// Returns true if the entity is referenced by at least one other row/object.
        /// </summary>
        Task<bool> IsInUseAsync(Guid entityId, CancellationToken ct = default);

        /// <summary>
        /// Returns an approximate or exact count of references. Useful for UI pre-flight.
        /// Implementations may return 0 when counting is too expensive for the scenario.
        /// </summary>
        Task<int> GetUsageCountAsync(Guid entityId, CancellationToken ct = default);

        /// <summary>
        /// Returns up to <paramref name="resultCount"/> sample referencing IDs (e.g., task ids).
        /// Useful for showing examples in the UI. Implementations can return an empty list.
        /// </summary>
        Task<IEnumerable<Guid>> GetUsageSampleIdsAsync(Guid entityId, int resultCount = 5, CancellationToken ct = default);
    }