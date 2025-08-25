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
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }

    public Guid CreatorId { get; private set; }

    // Members (for private/restricted folders)
    private readonly List<UserProjectFolder> _members = new();
    public IReadOnlyCollection<UserProjectFolder> Members => _members.AsReadOnly();

    // Child entities
    private readonly List<ProjectList> _lists = new();
    public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

    // Constructors
    private ProjectFolder() { } // For EF Core

    // Internal constructor - only called by parent ProjectSpace
    internal ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name, 
        string? description, Visibility visibility, int orderIndex, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Description = description;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
    }

    // === VALIDATION METHOD (for business rules) ===
    public static void ValidateForCreation(string name, string? description, string color, 
        Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        ValidateBasicInfo(name, description);
        ValidateColor(color);
        ValidateGuid(projectWorkspaceId, nameof(projectWorkspaceId));
        ValidateGuid(projectSpaceId, nameof(projectSpaceId));
        ValidateGuid(creatorId, nameof(creatorId));
    }

    // === SELF MANAGEMENT METHODS ===

    public void UpdateBasicInfo(string name, string? description)
    {
        // Normalize inputs first
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        
        // Check for changes first to avoid unnecessary work
        if (Name == name && Description == description) return;
        
        // Then validate
        ValidateBasicInfo(name, description);
        
        var oldName = Name;
        var oldDescription = Description;
        Name = name;
        Description = description;
        UpdateTimestamp();
        AddDomainEvent(new FolderBasicInfoUpdatedEvent(Id, oldName, name, oldDescription, description));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        // Check for changes first
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new FolderVisibilityChangedEvent(Id, oldVisibility, newVisibility));
        
        // CASCADE: Update all child lists to match folder visibility if they were public
        CascadeVisibilityToLists(newVisibility);
    }

    internal void UpdateOrderIndex(int newOrderIndex)
    {
        if (newOrderIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrderIndex), "Order index cannot be negative.");
        
        // Check for changes first
        if (OrderIndex == newOrderIndex) return;

        OrderIndex = newOrderIndex;
        UpdateTimestamp();
    }

    // === MEMBERSHIP MANAGEMENT ===

    public void AddMember(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));
        
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this folder.");

        var member = UserProjectFolder.Create(userId, Id);
        _members.Add(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToFolderEvent(Id, userId));
        
        // CASCADE: Add member to all child lists
        CascadeMembershipToLists(userId, isAdding: true);
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId)
            throw new InvalidOperationException("Cannot remove folder creator from folder.");
            
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this folder.");

        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromFolderEvent(Id, userId));
        
        // CASCADE: Remove member from all child lists
        CascadeMembershipToLists(userId, isAdding: false);
    }
    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new FolderArchivedEvent(Id));

        // CASCADE: Archive all child tasks
        ArchiveAllLists();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new FolderUnarchivedEvent(Id));

        // CASCADE: Unarchive all child tasks
        UnarchiveAllLists();
    }

    // === CHILD ENTITY MANAGEMENT ===

    public ProjectList CreateList(string name, string? description, string color, Visibility visibility)
    {
        // Normalize inputs
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        color = color?.Trim() ?? string.Empty;

        // Validate
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty.", nameof(name));
        ValidateColor(color);

        if (_lists.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A list with the name '{name}' already exists in this folder.");

        var orderIndex = _lists.Count;
        var list = new ProjectList(Guid.NewGuid(), ProjectWorkspaceId, ProjectSpaceId, Id,
            name, description, visibility, orderIndex, CreatorId);
        _lists.Add(list);

        UpdateTimestamp();
        AddDomainEvent(new ListCreatedInFolderEvent(Id, list.Id, name, CreatorId));
        return list;
    }

    public void RemoveList(Guid listId)
    {
        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null)
            throw new InvalidOperationException("List not found in this folder.");

        // TODO: Check if list has tasks - might need domain service

        _lists.Remove(list);
        UpdateTimestamp();
        AddDomainEvent(new ListRemovedFromFolderEvent(Id, listId, list.Name));
    }

    public void ReorderLists(List<Guid> listIds)
    {
        if (listIds.Count != _lists.Count)
            throw new ArgumentException("Must provide all list IDs for reordering.", nameof(listIds));

        var allListsExist = listIds.All(id => _lists.Any(l => l.Id == id));
        if (!allListsExist)
            throw new ArgumentException("One or more list IDs are invalid.", nameof(listIds));

        for (int i = 0; i < listIds.Count; i++)
        {
            var list = _lists.First(l => l.Id == listIds[i]);
            list.UpdateOrderIndex(i);
        }

        UpdateTimestamp();
        AddDomainEvent(new ListsReorderedInFolderEvent(Id, listIds));
    }

    // === BULK OPERATIONS (MISSING HIERARCHICAL METHODS) ===
    
    public void ArchiveAllLists()
    {
        if (!_lists.Any(l => !l.IsArchived)) return; // Nothing to archive
        
        foreach (var list in _lists.Where(l => !l.IsArchived))
        {
            list.Archive();
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllListsArchivedInFolderEvent(Id));
    }
    
    public void UnarchiveAllLists()
    {
        if (!_lists.Any(l => l.IsArchived)) return; // Nothing to unarchive
        
        foreach (var list in _lists.Where(l => l.IsArchived))
        {
            list.Unarchive();
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllListsUnarchivedInFolderEvent(Id));
    }

    // === LIST ATTACHMENT/DETACHMENT (for moving between containers) ===

    internal void AttachList(ProjectList list)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        if (list.ProjectWorkspaceId != ProjectWorkspaceId || list.ProjectSpaceId != ProjectSpaceId)
            throw new InvalidOperationException("List belongs to different workspace or space.");
        if (_lists.Any(l => l.Id == list.Id))
            return; // Already attached

        if (_lists.Any(l => l.Name.Equals(list.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A list with the name '{list.Name}' already exists in this folder.");

        _lists.Add(list);
        UpdateTimestamp();
    }

    internal void DetachList(Guid listId)
    {
        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null) return; // Not attached

        _lists.Remove(list);
        UpdateTimestamp();
    }

    // === PRIVATE CASCADE METHODS ===
    
    private void CascadeVisibilityToLists(Visibility newVisibility)
    {
        // Only cascade if making more restrictive (Public -> Private)
        if (newVisibility == Visibility.Private)
        {
            foreach (var list in _lists.Where(l => l.Visibility == Visibility.Public))
            {
                list.ChangeVisibility(Visibility.Private);
            }
        }
    }
    
    private void CascadeMembershipToLists(Guid userId, bool isAdding)
    {
        foreach (var list in _lists.Where(l => l.Visibility == Visibility.Private))
        {
            try
            {
                if (isAdding)
                    list.AddMember(userId);
                else
                    list.RemoveMember(userId);
            }
            catch (InvalidOperationException)
            {
                // Member might already exist or not exist - ignore
            }
        }
    }

    // === VALIDATION HELPER METHODS ===
    
    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Folder name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500)
            throw new ArgumentException("Folder description cannot exceed 500 characters.", nameof(description));
    }
    
    private static void ValidateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Folder color cannot be empty.", nameof(color));
        if (!IsValidColorCode(color))
            throw new ArgumentException("Invalid color format.", nameof(color));
    }
    
    private static void ValidateGuid(Guid guid, string parameterName)
    {
        if (guid == Guid.Empty)
            throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
    }

    private static bool IsValidColorCode(string color) =>
        !string.IsNullOrWhiteSpace(color) && 
        (color.StartsWith("#") && (color.Length == 7 || color.Length == 4));

    // === QUERY HELPER METHODS ===

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public bool CanUserAccess(Guid userId) => 
        Visibility == Visibility.Public || 
        userId == CreatorId || 
        HasMember(userId);

    public bool CanUserManage(Guid userId) => userId == CreatorId;

    public IEnumerable<ProjectList> GetOrderedLists() => 
        _lists.OrderBy(l => l.OrderIndex ?? int.MaxValue).ThenBy(l => l.CreatedAt);

    public int GetListCount() => _lists.Count;

    public bool HasListWithName(string name) => 
        _lists.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
    public int GetTotalTaskCount() => _lists.Sum(l => l.GetTaskCount());
    
    public int GetCompletedTaskCount() => _lists.Sum(l => l.GetCompletedTaskCount());
    
    public double GetCompletionPercentage() => 
        GetTotalTaskCount() == 0 ? 0 : (double)GetCompletedTaskCount() / GetTotalTaskCount() * 100;
}
