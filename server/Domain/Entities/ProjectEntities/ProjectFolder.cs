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
    
    // Child entities
    private readonly List<ProjectList> _lists = new();
    public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

    // Constructors
    private ProjectFolder() { } // For EF Core

    private ProjectFolder(Guid id, string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        Id = id;
        Name = name;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        CreatorId = creatorId;

        // Initialize other declared properties with default values
        Description = null;
        Color = "#FFFFFF"; // Default color
        OrderIndex = 0;
        Visibility = Visibility.Public; // Default visibility
    }

    // Static Factory Methods
    public static ProjectFolder Create(string name, Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));

        var folder = new ProjectFolder(Guid.NewGuid(), name, projectWorkspaceId, projectSpaceId, creatorId);
        folder.AddDomainEvent(new FolderCreatedEvent(folder.Id, folder.Name, folder.ProjectSpaceId, folder.CreatorId));

        

        return folder;
    }

    // Public Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Folder name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        AddDomainEvent(new FolderNameUpdatedEvent(Id, newName));
    }

    public void AddList(Guid listId, string name)
    {
        AddDomainEvent(new ListAddedToFolderEvent(Id, listId, name));
    }

    public void AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this folder.");

        _members.Add(UserProjectFolder.Create(userId, Id));
        AddDomainEvent(new MemberAddedToFolderEvent(Id, userId));
    }
}
