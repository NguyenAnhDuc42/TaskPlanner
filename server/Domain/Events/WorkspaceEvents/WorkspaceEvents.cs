using Domain.Common.Interfaces;
using Domain.Enums;
using System;

namespace Domain.Events.WorkspaceEvents;

public record WorkspaceCreatedEvent(Guid WorkspaceId, string WorkspaceName, Guid CreatorId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceNameUpdatedEvent(Guid WorkspaceId, string NewName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceDescriptionUpdatedEvent(Guid WorkspaceId, string NewDescription) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record SpaceAddedToWorkspaceEvent(Guid WorkspaceId, Guid SpaceId, string SpaceName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record MemberAddedToWorkspaceEvent(Guid WorkspaceId, Guid UserId, Role Role) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record DefaultStatusAddedToWorkspaceEvent(Guid WorkspaceId, Guid StatusId, string StatusName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

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