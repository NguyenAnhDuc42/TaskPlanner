using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Events.WorkspaceEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectWorkspace;

public class ProjectFolder : Aggregate
{
    // Public Properties
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    
    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }

    // Navigation Properties
    private readonly List<ProjectList> _lists = new();
    public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

    private readonly List<UserProjectFolder> _members = new();
    public IReadOnlyCollection<UserProjectFolder> Members => _members.AsReadOnly();

    // Constructors
    private ProjectFolder() { } // For EF Core

    private ProjectFolder(Guid id, string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        Id = id;
        Name = name;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        CreatorId = creatorId;
        IsPrivate = false; // Default
        IsArchived = false; // Default
    }

    // Static Factory Methods
    public static ProjectFolder Create(string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));

        var folder = new ProjectFolder(Guid.NewGuid(), name, projectWorkspaceId, projectSpaceId, creatorId);
        // No direct event for folder creation, as it's part of Space aggregate

        // Automatically create a default list
        var defaultList = ProjectList.Create("List", projectWorkspaceId, projectSpaceId, folder.Id, creatorId);
        folder._lists.Add(defaultList);
        // No direct event for list creation from folder, handled by Space aggregate

        return folder;
    }

    // Public Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Folder name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        // No direct event for folder name update, handled by Space aggregate
    }

    public ProjectList AddList(string name, Guid creatorId)
    {
        var list = ProjectList.Create(name, ProjectWorkspaceId, ProjectSpaceId, Id, creatorId);
        _lists.Add(list);
        // No direct event for list creation from folder, handled by Space aggregate
        return list;
    }
}
