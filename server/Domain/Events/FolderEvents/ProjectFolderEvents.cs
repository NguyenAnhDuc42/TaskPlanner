using Domain.Common.Interfaces;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.Events.FolderEvents;

public record FolderBasicInfoUpdatedEvent(Guid FolderId, string OldName, string NewName, string? OldDescription, string? NewDescription) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record FolderColorUpdatedEvent(Guid FolderId, string OldColor, string NewColor) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record FolderVisibilityChangedEvent(Guid FolderId, Visibility OldVisibility, Visibility NewVisibility) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record MemberAddedToFolderEvent(Guid FolderId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record MemberRemovedFromFolderEvent(Guid FolderId, Guid UserId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record FolderArchivedEvent(Guid FolderId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record FolderUnarchivedEvent(Guid FolderId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record ListCreatedInFolderEvent(Guid FolderId, Guid ListId, string ListName, Guid CreatorId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record ListRemovedFromFolderEvent(Guid FolderId, Guid ListId, string ListName) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record ListsReorderedInFolderEvent(Guid FolderId, List<Guid> ListIds) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record AllListsArchivedInFolderEvent(Guid FolderId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record AllListsUnarchivedInFolderEvent(Guid FolderId) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

public record AllListsColorUpdatedInFolderEvent(Guid FolderId, string NewColor) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}