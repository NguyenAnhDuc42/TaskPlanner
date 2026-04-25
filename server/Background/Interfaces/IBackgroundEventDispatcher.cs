using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common.Interfaces;

namespace Background.Interfaces;

public interface IBackgroundEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
