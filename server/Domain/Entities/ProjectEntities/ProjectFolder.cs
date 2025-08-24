using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.FolderEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectFolder : Aggregate
{
    public Guid ProjectWorkspaceId { get; private set; } // For quick queries
    public Guid ProjectSpaceId { get; private set; } // Parent reference
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Color { get; private set; } = null!;
    public int OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    public Guid CreatorId { get; private set; }

    // Members (for private/restricted folders)
    private readonly List<UserProjectFolder> _members = new();
    public IReadOnlyCollection<UserProjectFolder> Members => _members.AsReadOnly();

    // Child entities: lists that belong to this folder (keeps folder state consistent)
    private readonly List<ProjectList> _lists = new();
    public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

    // Constructors
    private ProjectFolder() { } // For EF Core

    // Internal constructor used by ProjectSpace.CreateFolder
    internal ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name, string? description,
        string color, Visibility visibility, int orderIndex, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Description = description;
        Color = string.IsNullOrWhiteSpace(color) ? "#FFFFFF" : color;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
    }

    // Factory (alternate path)
    public static ProjectFolder Create(string name, Guid projectWorkspaceId, Guid projectSpaceId, string color, Visibility visibility, int orderIndex, Guid creatorId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));
        if (projectWorkspaceId == Guid.Empty) throw new ArgumentException("Project workspace id cannot be empty.", nameof(projectWorkspaceId));
        if (projectSpaceId == Guid.Empty) throw new ArgumentException("Project space id cannot be empty.", nameof(projectSpaceId));
        if (creatorId == Guid.Empty) throw new ArgumentException("Creator id cannot be empty.", nameof(creatorId));

        var folder = new ProjectFolder(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, name, description, color ?? "#FFFFFF", visibility, orderIndex, creatorId);
        folder.AddDomainEvent(new FolderCreatedEvent(folder.Id, folder.Name, folder.ProjectSpaceId, folder.CreatorId));
        return folder;
    }

    // Basic updates
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Folder name cannot be empty.", nameof(newName));
        if (Name == newName) return;
        Name = newName;
        UpdateTimestamp();
        AddDomainEvent(new FolderNameUpdatedEvent(Id, newName));
    }

    public void UpdateDescription(string? newDescription)
    {
        if (Description == newDescription) return;
        if (newDescription != null && string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("Folder description cannot be whitespace.", nameof(newDescription));

        Description = newDescription;
        UpdateTimestamp();
        AddDomainEvent(new FolderDescriptionUpdatedEvent(Id, newDescription));
    }

    public void UpdateVisualSettings(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Folder color cannot be empty.", nameof(color));
        if (Color == color) return;
        var oldColor = Color;
        Color = color;
        UpdateTimestamp();
        AddDomainEvent(new FolderVisualSettingsUpdatedEvent(Id, color, oldColor));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new FolderVisibilityChangedEvent(Id, newVisibility));
    }

    internal void UpdateOrderIndex(int newOrderIndex)
    {
        if (newOrderIndex < 0) throw new ArgumentOutOfRangeException(nameof(newOrderIndex), "Order index cannot be negative.");
        if (OrderIndex == newOrderIndex) return;
        OrderIndex = newOrderIndex;
        UpdateTimestamp();
        AddDomainEvent(new FolderOrderIndexUpdatedEvent(Id, newOrderIndex));
    }

    // Member management
    public void AddMember(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (_members.Any(m => m.UserId == userId)) throw new InvalidOperationException("User is already a member of this folder.");
        _members.Add(UserProjectFolder.Create(userId, Id));
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToFolderEvent(Id, userId));
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId) throw new InvalidOperationException("Cannot remove folder creator from folder.");
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new InvalidOperationException("User is not a member of this folder.");
        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromFolderEvent(Id, userId));
    }

    // ---- List attach / detach so folder state stays in sync ----
    internal void AttachList(ProjectList list)
    {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (list.ProjectWorkspaceId != ProjectWorkspaceId || list.ProjectSpaceId != ProjectSpaceId)
            throw new InvalidOperationException("List parent ids do not match this folder's parents.");

        if (_lists.Any(l => l.Id == list.Id)) return; // already attached

        // Validate duplicate names inside this folder
        if (_lists.Any(l => l.Name.Equals(list.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A list with the name '{list.Name}' already exists in this folder.");

        _lists.Add(list);
        UpdateTimestamp();
        AddDomainEvent(new ListAttachedToFolderEvent(Id, list.Id)); // create event if needed
    }

    internal void DetachList(Guid listId)
    {
        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null) return; // not attached
        _lists.Remove(list);
        UpdateTimestamp();
        AddDomainEvent(new ListDetachedFromFolderEvent(Id, listId)); // create event if needed
    }

    // Query helpers
    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);
    public bool CanUserAccess(Guid userId) => Visibility == Visibility.Public || userId == CreatorId || HasMember(userId);
    public bool CanUserManage(Guid userId) => userId == CreatorId;
}
