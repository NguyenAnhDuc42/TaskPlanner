using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Events.WorkspaceEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectWorkspace;

public class ProjectList : Aggregate
{
    // Public Properties
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    
    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; }
    public int OrderIndex { get; private set; }
    public Guid CreatorId { get; private set; }
    public Guid? DefaultAssigneeId { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }

    // Navigation Properties
    private readonly List<Guid> _taskIds = new(); // Store Task IDs as Task is an aggregate root
    public IReadOnlyCollection<Guid> TaskIds => _taskIds.AsReadOnly();

    private readonly List<UserProjectList> _members = new();
    public IReadOnlyCollection<UserProjectList> Members => _members.AsReadOnly();

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
        IsPrivate = false; // Default
        IsArchived = false; // Default
        OrderIndex = 0; // Default
    }

    // Static Factory Methods
    public static ProjectList Create(string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty.", nameof(name));

        var list = new ProjectList(Guid.NewGuid(), name, projectWorkspaceId, projectSpaceId, projectFolderId, creatorId);
        // No direct event for list creation, as it's part of Space/Folder aggregate
        return list;
    }

    // Public Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("List name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        // No direct event for list name update, handled by Space/Folder aggregate
    }

    public void AddTask(Guid taskId)
    {
        if (_taskIds.Contains(taskId))
            throw new InvalidOperationException("Task already exists in this list.");

        _taskIds.Add(taskId);
        // No direct event for task added to list, handled by Task aggregate or higher level
    }
}