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
