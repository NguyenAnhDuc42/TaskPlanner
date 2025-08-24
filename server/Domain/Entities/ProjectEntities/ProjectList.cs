using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.ListEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectList : Aggregate
{
    public Guid ProjectWorkspaceId { get; private set; } // For quick queries
    public Guid ProjectSpaceId { get; private set; } // Parent reference
    public Guid? ProjectFolderId { get; private set; } // Optional parent
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Color { get; private set; } = null!;
    public int OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    public Guid CreatorId { get; private set; }
    public bool IsArchived { get; private set; }

    // Dates
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }

    // Members (for private/restricted lists)
    private readonly List<UserProjectList> _members = new();
    public IReadOnlyCollection<UserProjectList> Members => _members.AsReadOnly();

    // Child references (task ids)
    private readonly List<Guid> _taskIds = new();
    public IReadOnlyCollection<Guid> TaskIds => _taskIds.AsReadOnly();

    // Constructors
    private ProjectList() { } // For EF Core

    // Internal constructor matching ProjectSpace.CreateList usage
    internal ProjectList(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId, string name, string? description,
        string color, Visibility visibility, int orderIndex, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name;
        Description = description;
        Color = string.IsNullOrWhiteSpace(color) ? "#FFFFFF" : color;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
        IsArchived = false;
        StartDate = null;
        DueDate = null;
    }

    // Factory (alternate)
    public static ProjectList Create(string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId, string color, Visibility visibility, int orderIndex, Guid creatorId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("List name cannot be empty.", nameof(name));
        if (projectWorkspaceId == Guid.Empty) throw new ArgumentException("Project workspace id cannot be empty.", nameof(projectWorkspaceId));
        if (projectSpaceId == Guid.Empty) throw new ArgumentException("Project space id cannot be empty.", nameof(projectSpaceId));
        if (creatorId == Guid.Empty) throw new ArgumentException("Creator id cannot be empty.", nameof(creatorId));

        var list = new ProjectList(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, projectFolderId, name, description, color ?? "#FFFFFF", visibility, orderIndex, creatorId);
        list.AddDomainEvent(new ListCreatedEvent(list.Id, list.Name, list.ProjectSpaceId, list.ProjectFolderId, list.CreatorId));
        return list;
    }

    // Updates
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("List name cannot be empty.", nameof(newName));
        if (Name == newName) return;
        Name = newName;
        UpdateTimestamp();
        AddDomainEvent(new ListNameUpdatedEvent(Id, newName));
    }

    public void UpdateDescription(string? newDescription)
    {
        if (Description == newDescription) return;
        if (newDescription != null && string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("List description cannot be whitespace.", nameof(newDescription));

        Description = newDescription;
        UpdateTimestamp();
        AddDomainEvent(new ListDescriptionUpdatedEvent(Id, newDescription));
    }

    public void UpdateVisualSettings(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("List color cannot be empty.", nameof(color));
        if (Color == color) return;
        var oldColor = Color;
        Color = color;
        UpdateTimestamp();
        AddDomainEvent(new ListVisualSettingsUpdatedEvent(Id, color, oldColor));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new ListVisibilityChangedEvent(Id, newVisibility));
    }

    public void Archive()
    {
        if (IsArchived) return;
        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new ListArchivedEvent(Id));
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new ListUnarchivedEvent(Id));
    }

    // ---- Move between folder / null folder ----
    // Returns the previous folder id (so the caller can update parent folder collections)
    public Guid? MoveToFolder(Guid? newFolderId)
    {
        if (ProjectFolderId == newFolderId) return ProjectFolderId; // no-op, return current (unchanged)

        var old = ProjectFolderId;
        ProjectFolderId = newFolderId;
        UpdateTimestamp();
        AddDomainEvent(new ListMovedEvent(Id, old, newFolderId)); // create `ListMovedEvent` if you want a list-level event
        return old;
    }

    // Tasks
    public void AddTask(Guid taskId)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("Task id cannot be empty.", nameof(taskId));
        if (IsArchived) throw new InvalidOperationException("Cannot add tasks to an archived list.");
        if (_taskIds.Contains(taskId)) throw new InvalidOperationException("Task already exists in this list.");

        _taskIds.Add(taskId);
        UpdateTimestamp();
        AddDomainEvent(new TaskAddedToListEvent(Id, taskId));
    }

    public void RemoveTask(Guid taskId)
    {
        if (taskId == Guid.Empty) throw new ArgumentException("Task id cannot be empty.", nameof(taskId));
        if (!_taskIds.Remove(taskId)) throw new InvalidOperationException("Task not found in this list.");
        UpdateTimestamp();
        AddDomainEvent(new TaskRemovedFromListEvent(Id, taskId));
    }

    public void UpdateOrderIndex(int newOrderIndex)
    {
        if (newOrderIndex < 0) throw new ArgumentOutOfRangeException(nameof(newOrderIndex), "Order index cannot be negative.");
        if (OrderIndex == newOrderIndex) return;
        OrderIndex = newOrderIndex;
        UpdateTimestamp();
        AddDomainEvent(new ListOrderIndexUpdatedEvent(Id, newOrderIndex));
    }

    // Members
    public void AddMember(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (_members.Any(m => m.UserId == userId)) throw new InvalidOperationException("User is already a member of this list.");
        _members.Add(UserProjectList.Create(userId, Id));
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToListEvent(Id, userId));
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId) throw new InvalidOperationException("Cannot remove list creator from list.");
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) throw new InvalidOperationException("User is not a member of this list.");
        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromListEvent(Id, userId));
    }

    // Query helpers
    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);
    public bool CanUserAccess(Guid userId) => Visibility == Visibility.Public || userId == CreatorId || HasMember(userId);
    public bool CanUserManage(Guid userId) => userId == CreatorId;
}
