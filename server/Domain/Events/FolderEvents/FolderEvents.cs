using Domain.Common.Interfaces;
using System;

namespace Domain.Events.FolderEvents;

public record FolderCreatedEvent(Guid FolderId, string FolderName, Guid ProjectSpaceId, Guid CreatorId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record FolderNameUpdatedEvent(Guid FolderId, string NewName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record ListAddedToFolderEvent(Guid FolderId, Guid ListId, string ListName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record MemberAddedToFolderEvent(Guid FolderId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}