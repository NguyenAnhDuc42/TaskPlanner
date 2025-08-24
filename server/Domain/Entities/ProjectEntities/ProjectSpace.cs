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
    public Guid ProjectWorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Icon { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public Visibility Visibility { get; private set; }
    public int? OrderIndex { get; private set; } // Nullable for proper ordering
    public Guid CreatorId { get; private set; }
    
    // Members (for private/restricted spaces) - no roles, just access
    private readonly List<UserProjectSpace> _members = new();
    public IReadOnlyCollection<UserProjectSpace> Members => _members.AsReadOnly();
    
    // Child entities
    private readonly List<ProjectFolder> _folders = new();
    public IReadOnlyCollection<ProjectFolder> Folders => _folders.AsReadOnly();
    
    private readonly List<ProjectList> _lists = new();
    public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

    // Constructors
    private ProjectSpace() { } // For EF Core

    // Internal constructor - only called by parent workspace
    internal ProjectSpace(Guid id, Guid projectWorkspaceId, string name, string? description, 
        string icon, string color, Visibility visibility, int orderIndex, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        Name = name;
        Description = description;
        Icon = icon;
        Color = color;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
    }

    // === SELF MANAGEMENT METHODS ===
    
    public void UpdateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Space name cannot be empty.", nameof(name));
        
        var changed = Name != name || Description != description;
        if (!changed) return;

        Name = name;
        Description = description;
        UpdateTimestamp();
        AddDomainEvent(new SpaceBasicInfoUpdatedEvent(Id, name, description));
    }

    public void UpdateVisualSettings(string icon, string color)
    {
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Space icon cannot be empty.", nameof(icon));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Space color cannot be empty.", nameof(color));
        
        var changed = Icon != icon || Color != color;
        if (!changed) return;

        Icon = icon;
        Color = color;
        UpdateTimestamp();
        AddDomainEvent(new SpaceVisualSettingsUpdatedEvent(Id, icon, color));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new SpaceVisibilityChangedEvent(Id, newVisibility));
    }

    internal void UpdateOrderIndex(int newOrderIndex)
    {
        if (OrderIndex == newOrderIndex) return;
        
        OrderIndex = newOrderIndex;
        UpdateTimestamp();
    }

    // === MEMBERSHIP MANAGEMENT ===
    
    public void AddMember(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this space.");

        var member = UserProjectSpace.Create(userId, Id);
        _members.Add(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToSpaceEvent(Id, userId));
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId)
            throw new InvalidOperationException("Cannot remove space creator from space.");
            
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this space.");

        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromSpaceEvent(Id, userId));
    }

    // === CHILD ENTITY MANAGEMENT ===
    
    public ProjectFolder CreateFolder(string name, string? description, string color, Visibility visibility)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Folder color cannot be empty.", nameof(color));

        if (_folders.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A folder with the name '{name}' already exists in this space.");

        var orderIndex = _folders.Count;
        var folder = new ProjectFolder(Guid.NewGuid(), ProjectWorkspaceId, Id, name, description, 
            color, visibility, orderIndex, CreatorId);
        _folders.Add(folder);
        
        UpdateTimestamp();
        AddDomainEvent(new FolderCreatedInSpaceEvent(Id, folder.Id, name, CreatorId));
        return folder;
    }

    public void RemoveFolder(Guid folderId)
    {
        var folder = _folders.FirstOrDefault(f => f.Id == folderId);
        if (folder == null)
            throw new InvalidOperationException("Folder not found in this space.");

        // TODO: Check if folder has lists - might need domain service
        // For now, assume it's handled by application service

        _folders.Remove(folder);
        UpdateTimestamp();
        AddDomainEvent(new FolderRemovedFromSpaceEvent(Id, folderId, folder.Name));
    }

    public void ReorderFolders(List<Guid> folderIds)
    {
        if (folderIds.Count != _folders.Count)
            throw new ArgumentException("Must provide all folder IDs for reordering.");

        var allFoldersExist = folderIds.All(id => _folders.Any(f => f.Id == id));
        if (!allFoldersExist)
            throw new ArgumentException("One or more folder IDs are invalid.");

        for (int i = 0; i < folderIds.Count; i++)
        {
            var folder = _folders.First(f => f.Id == folderIds[i]);
            folder.UpdateOrderIndex(i);
        }

        UpdateTimestamp();
        AddDomainEvent(new FoldersReorderedInSpaceEvent(Id, folderIds));
    }

    public ProjectList CreateList(string name, string? description, string color, Visibility visibility, Guid? folderId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("List color cannot be empty.", nameof(color));

        // If folder specified, validate it exists in this space
        if (folderId.HasValue)
        {
            var folder = _folders.FirstOrDefault(f => f.Id == folderId.Value);
            if (folder == null)
                throw new InvalidOperationException("Specified folder not found in this space.");
        }

        // Check for duplicate names in same container (space or folder)
        var existingLists = folderId.HasValue 
            ? _lists.Where(l => l.ProjectFolderId == folderId) 
            : _lists.Where(l => l.ProjectFolderId == null);
            
        if (existingLists.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A list with the name '{name}' already exists in this container.");

        var orderIndex = existingLists.Count();
        var list = new ProjectList(Guid.NewGuid(), ProjectWorkspaceId, Id, folderId, name, 
            description, color, visibility, orderIndex, CreatorId);
        _lists.Add(list);
        
        UpdateTimestamp();
        AddDomainEvent(new ListCreatedInSpaceEvent(Id, list.Id, name, folderId, CreatorId));
        return list;
    }

    public void RemoveList(Guid listId)
    {
        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null)
            throw new InvalidOperationException("List not found in this space.");

        // TODO: Check if list has tasks - might need domain service

        _lists.Remove(list);
        UpdateTimestamp();
        AddDomainEvent(new ListRemovedFromSpaceEvent(Id, listId, list.Name));
    }

    public void MoveListToFolder(Guid listId, Guid? newFolderId)
    {
        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null)
            throw new InvalidOperationException("List not found in this space.");

        if (newFolderId.HasValue)
        {
            var folder = _folders.FirstOrDefault(f => f.Id == newFolderId.Value);
            if (folder == null)
                throw new InvalidOperationException("Target folder not found in this space.");
        }

        if (list.ProjectFolderId == newFolderId) return;

        list.MoveToFolder(newFolderId);
        UpdateTimestamp();
        AddDomainEvent(new ListMovedToFolderEvent(Id, listId, list.ProjectFolderId, newFolderId));
    }

    // === QUERY HELPER METHODS ===
    
    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);
    
    public bool CanUserAccess(Guid userId) => 
        Visibility == Visibility.Public || 
        userId == CreatorId || 
        HasMember(userId);
        
    public bool CanUserManage(Guid userId) => userId == CreatorId;
    
    public IEnumerable<ProjectList> GetListsInFolder(Guid? folderId) => 
        _lists.Where(l => l.ProjectFolderId == folderId);
        
    public IEnumerable<ProjectFolder> GetOrderedFolders() => 
        _folders.OrderBy(f => f.OrderIndex ?? int.MaxValue).ThenBy(f => f.CreatedAt);
        
    public IEnumerable<ProjectList> GetOrderedLists(Guid? folderId = null) => 
        GetListsInFolder(folderId).OrderBy(l => l.OrderIndex ?? int.MaxValue).ThenBy(l => l.CreatedAt);
}