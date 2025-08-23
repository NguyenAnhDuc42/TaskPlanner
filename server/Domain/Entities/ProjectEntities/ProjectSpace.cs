using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.SpaceEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

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
        
        // New: Validation for icon and color
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Space icon cannot be empty.", nameof(icon));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Space color cannot be empty.", nameof(color));

        var space = new ProjectSpace(Guid.NewGuid(), name, icon, color, projectWorkspaceId, creatorId);
        space.AddDomainEvent(new SpaceCreatedEvent(space.Id, space.Name, space.ProjectWorkspaceId, space.CreatorId));
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

    public void AddFolder(Guid folderId, string name)
    {
        AddDomainEvent(new FolderAddedToSpaceEvent(Id, folderId, name));
    }

    public void AddList(Guid listId, string name, Guid? projectFolderId = null)
    {
        AddDomainEvent(new ListAddedToSpaceEvent(Id, listId, name));
    }

    public void AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this space.");

        _members.Add(UserProjectSpace.Create(userId, Id));
        AddDomainEvent(new MemberAddedToSpaceEvent(Id, userId));
    }
}