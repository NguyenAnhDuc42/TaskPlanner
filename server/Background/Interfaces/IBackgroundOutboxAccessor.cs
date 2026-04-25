using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Background.Interfaces;

public interface IBackgroundOutboxAccessor
{
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    
    void ResetTracking(OutboxMessage message);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
