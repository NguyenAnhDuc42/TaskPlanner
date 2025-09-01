
using Domain.Common.Interfaces;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.Events.WorkspaceEvents;

public record WorkspaceCreatedEvent(Guid WorkspaceId, string WorkspaceName, Guid CreatorId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceUpdatedEvent(Guid WorkspaceId, string? Name, string? Description, string? Color, string? Icon, Visibility? Visibility, bool? IsArchived) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceBasicInfoUpdatedEvent(Guid WorkspaceId, string OldName, string NewName, string? OldDescription, string? NewDescription) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceVisualSettingsUpdatedEvent(Guid WorkspaceId, string NewColor, string NewIcon) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceVisibilityChangedEvent(Guid WorkspaceId, Visibility NewVisibility) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceArchivedEvent(Guid WorkspaceId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceUnarchivedEvent(Guid WorkspaceId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceJoinCodeChangedEvent(Guid WorkspaceId, string NewJoinCode, string OldJoinCode) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record MemberAddedToWorkspaceEvent(Guid WorkspaceId, Guid UserId, Role Role) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record MemberRemovedFromWorkspaceEvent(Guid WorkspaceId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceMemberRoleChangedEvent(Guid WorkspaceId, Guid UserId, Role NewRole) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record WorkspaceOwnershipTransferredEvent(Guid WorkspaceId, Guid OldOwnerId, Guid NewOwnerId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record StatusCreatedInWorkspaceEvent(Guid WorkspaceId, Guid StatusId, string StatusName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record StatusUpdatedInWorkspaceEvent(Guid WorkspaceId, Guid StatusId, string OldName, string NewName, string OldColor, string NewColor) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record StatusDeletedFromWorkspaceEvent(Guid WorkspaceId, Guid StatusId, string StatusName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record StatusesReorderedInWorkspaceEvent(Guid WorkspaceId, List<Guid> OrderedStatusIds) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record SpaceCreatedInWorkspaceEvent(Guid WorkspaceId, Guid SpaceId, string SpaceName, Guid CreatorId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record SpaceRemovedFromWorkspaceEvent(Guid WorkspaceId, Guid SpaceId, string SpaceName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record SpacesReorderedInWorkspaceEvent(Guid WorkspaceId, List<Guid> OrderedSpaceIds) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record AllSpacesArchivedInWorkspaceEvent(Guid WorkspaceId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
public record AllSpacesUnarchivedInWorkspaceEvent(Guid WorkspaceId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
