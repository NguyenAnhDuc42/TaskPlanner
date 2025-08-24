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
    
    // List-specific properties
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    
    // Members (for private/restricted lists)
    private readonly List<UserProjectList> _members = new();
    public IReadOnlyCollection<UserProjectList> Members => _members.AsReadOnly();
    
    // Child entities
    private readonly List<ProjectTask> _tasks = new();
    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

    // Constructors
    private ProjectList() { } // For EF Core

    private ProjectList(Guid id, string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId, Guid creatorId)
    {
        Id = id;
        Name = name;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        CreatorId = creatorId;

        // Initialize other declared properties with default values
        Description = null;
        Color = "#FFFFFF"; // Default color
        OrderIndex = 0; // Already set, but explicitly here for clarity
        Visibility = Visibility.Public; // Default visibility
        StartDate = null;
        DueDate = null;
    }

    // Static Factory Methods
    public static ProjectList Create(string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty.", nameof(name));

        var list = new ProjectList(Guid.NewGuid(), name, projectWorkspaceId, projectSpaceId, projectFolderId, creatorId);
        list.AddDomainEvent(new ListCreatedEvent(list.Id, list.Name, list.ProjectSpaceId, list.ProjectFolderId, list.CreatorId));
        return list;
    }

    // Public Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("List name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        AddDomainEvent(new ListNameUpdatedEvent(Id, newName));
    }

    public void AddTask(Guid taskId)
    {
        if (IsArchived) // New: Cannot add tasks to an archived list
            throw new InvalidOperationException("Cannot add tasks to an archived list.");

        if (_taskIds.Contains(taskId))
            throw new InvalidOperationException("Task already exists in this list.");

        _taskIds.Add(taskId);
        AddDomainEvent(new TaskAddedToListEvent(Id, taskId));
    }

    public void UpdateOrderIndex(int newOrderIndex)
    {
        if (newOrderIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrderIndex), "Order index cannot be negative.");
        if (OrderIndex == newOrderIndex) return;

        OrderIndex = newOrderIndex;
        AddDomainEvent(new ListOrderIndexUpdatedEvent(Id, newOrderIndex));
    }

    public void AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this list.");

        _members.Add(UserProjectList.Create(userId, Id));
        AddDomainEvent(new MemberAddedToListEvent(Id, userId));
    }
}
