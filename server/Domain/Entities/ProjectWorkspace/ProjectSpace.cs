using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.WorkspaceEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectWorkspace;

public class ProjectSpace : Aggregate
{
    // Public Properties
    public Guid ProjectWorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Icon { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public bool IsPrivate { get; private set; }
    public bool IsPublic { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }
    public SpaceFeatures EnabledFeatures { get; private set; }

    // Navigation Properties
    private readonly List<ProjectList> _lists = new();
    public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

    private readonly List<ProjectFolder> _folders = new();
    public IReadOnlyCollection<ProjectFolder> Folders => _folders.AsReadOnly();

    private readonly List<UserProjectSpace> _members = new();
    public IReadOnlyCollection<UserProjectSpace> Members => _members.AsReadOnly();

    // Constructors
    private ProjectSpace() { } // For EF Core

    private ProjectSpace(Guid id, string name, string icon, string color, Guid projectWorkspaceId, Guid creatorId)
    {
        Id = id;
        Name = name;
        Icon = icon;
        Color = color;
        ProjectWorkspaceId = projectWorkspaceId;
        CreatorId = creatorId;
        IsPrivate = false; // Default
        IsPublic = true; // Default
        IsArchived = false; // Default
        EnabledFeatures = SpaceFeatures.All; // Default
    }

    // Static Factory Methods
    public static ProjectSpace Create(string name, string icon, string color, Guid projectWorkspaceId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Space name cannot be empty.", nameof(name));

        var space = new ProjectSpace(Guid.NewGuid(), name, icon, color, projectWorkspaceId, creatorId);
        space.AddDomainEvent(new SpaceCreatedEvent(space.Id, space.Name, space.ProjectWorkspaceId, space.CreatorId));
        
        // Automatically create a default list
        var defaultList = ProjectList.Create("List", projectWorkspaceId, space.Id, null, creatorId);
        space._lists.Add(defaultList);
        space.AddDomainEvent(new ListAddedToSpaceEvent(space.Id, defaultList.Id, defaultList.Name));

        return space;
    }

    // Public Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Space name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        AddDomainEvent(new SpaceNameUpdatedEvent(Id, newName));
    }

    public ProjectFolder AddFolder(string name, Guid creatorId)
    {
        var folder = ProjectFolder.Create(name, ProjectWorkspaceId, Id, creatorId);
        _folders.Add(folder);
        AddDomainEvent(new FolderAddedToSpaceEvent(Id, folder.Id, folder.Name));
        return folder;
    }

    public ProjectList AddList(string name, Guid creatorId, Guid? projectFolderId = null)
    {
        var list = ProjectList.Create(name, ProjectWorkspaceId, Id, projectFolderId, creatorId);
        _lists.Add(list);
        AddDomainEvent(new ListAddedToSpaceEvent(Id, list.Id, list.Name));
        return list;
    }

    public void AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this space.");

        _members.Add(new UserProjectSpace { UserId = userId, ProjectSpaceId = Id });
        AddDomainEvent(new MemberAddedToSpaceEvent(Id, userId));
    }
}