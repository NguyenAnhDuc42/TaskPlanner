using Domain.Common.Interfaces;

namespace Domain.Events.Membership;

public record WorkspaceMembersRemovedBulkEvent(Guid WorkspaceId, IEnumerable<Guid> UserIds) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public Guid? AggregateId => WorkspaceId;
    public long SequenceNumber { get; init; }
}
