using Domain.Common.Interfaces;
using Domain.Enums;


namespace Domain.Events.Membership;

public record AddedMemberRecord(Guid UserId, Role Role);

public record WorkspaceMembersAddedBulkEvent(Guid WorkspaceId, IEnumerable<AddedMemberRecord> AddedMembers) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public Guid? AggregateId => WorkspaceId;
    public long SequenceNumber { get; init; }
}
