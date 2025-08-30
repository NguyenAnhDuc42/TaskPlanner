using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.FolderEvents;
using static Domain.Common.ColorValidator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectFolder : Aggregate
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }

    private readonly List<UserProjectFolder> _members = new();
    public IReadOnlyCollection<UserProjectFolder> Members => _members.AsReadOnly();

    private ProjectFolder() { } // For EF Core

    internal ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name,
        string? description, Visibility visibility, int orderIndex, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Description = description;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
    }

    // === VALIDATION METHOD ===
    public static void ValidateForCreation(string name, string? description, string color,
        Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        ValidateBasicInfo(name, description);
        ValidateColor(color);
        ValidateGuid(projectWorkspaceId, nameof(projectWorkspaceId));
        ValidateGuid(projectSpaceId, nameof(projectSpaceId));
        ValidateGuid(creatorId, nameof(creatorId));
    }

    // === SELF MANAGEMENT METHODS ===
    public void UpdateBasicInfo(string name, string? description)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (Name == name && Description == description) return;

        ValidateBasicInfo(name, description);

        var oldName = Name;
        var oldDescription = Description;
        Name = name;
        Description = description;
        UpdateTimestamp();
        AddDomainEvent(new FolderBasicInfoUpdatedEvent(Id, oldName, name, oldDescription, description));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new FolderVisibilityChangedEvent(Id, oldVisibility, newVisibility));
    }

    internal void UpdateOrderIndex(int newOrderIndex)
    {
        if (newOrderIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrderIndex), "Order index cannot be negative.");

        if (OrderIndex == newOrderIndex) return;

        OrderIndex = newOrderIndex;
        UpdateTimestamp();
    }

    // === MEMBERSHIP MANAGEMENT ===
    public void AddMember(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this folder.");

        var member = UserProjectFolder.Create(userId, Id);
        _members.Add(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToFolderEvent(Id, userId));
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId)
            throw new InvalidOperationException("Cannot remove folder creator from folder.");

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this folder.");

        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromFolderEvent(Id, userId));
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new FolderArchivedEvent(Id));
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new FolderUnarchivedEvent(Id));
    }

    // === VALIDATION HELPERS ===
    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Folder name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500)
            throw new ArgumentException("Folder description cannot exceed 500 characters.", nameof(description));
    }

    private static void ValidateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Folder color cannot be empty.", nameof(color));
        if (!IsValidColorCode(color))
            throw new ArgumentException("Invalid color format.", nameof(color));
    }

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}
