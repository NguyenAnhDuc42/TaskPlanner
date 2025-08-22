using Domain.Common.Interfaces;
using System;

namespace Domain.Events.SpaceEvents;

public record SpaceCreatedEvent(Guid SpaceId, string SpaceName, Guid ProjectWorkspaceId, Guid CreatorId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record SpaceNameUpdatedEvent(Guid SpaceId, string NewName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record FolderAddedToSpaceEvent(Guid SpaceId, Guid FolderId, string FolderName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record ListAddedToSpaceEvent(Guid SpaceId, Guid ListId, string ListName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record MemberAddedToSpaceEvent(Guid SpaceId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}