using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using Domain.Events.WorkspaceEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectWorkspace : Aggregate
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string JoinCode { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public string Icon { get; private set; } = null!;
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }
    
    // Workflow: Each workspace defines its task statuses
    private readonly List<Status> _statuses = new();
    public IReadOnlyCollection<Status> Statuses => _statuses.AsReadOnly();
    
    // Members with roles
    private readonly List<UserProjectWorkspace> _members = new();
    public IReadOnlyCollection<UserProjectWorkspace> Members => _members.AsReadOnly();
    
    // Child entities
    private readonly List<ProjectSpace> _spaces = new();
    public IReadOnlyCollection<ProjectSpace> Spaces => _spaces.AsReadOnly();

    // Constructors
    private ProjectWorkspace() { } // For EF Core

    private ProjectWorkspace(Guid id, string name, string? description, string joinCode, string color, string icon, Guid creatorId, Visibility visibility)
    {
        Id = id;
        Name = name;
        Description = description;
        JoinCode = joinCode;
        Color = color;
        Icon = icon;
        CreatorId = creatorId;
        Visibility = visibility;
        IsArchived = false;
    }

    // Static Factory Method
    public static ProjectWorkspace Create(string name, string? description, string color, string icon, Guid creatorId, Visibility visibility)
    {
        // Normalize inputs first
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        color = color?.Trim() ?? string.Empty;
        icon = icon?.Trim() ?? string.Empty;
        
        // Validate
        ValidateBasicInfo(name, description);
        ValidateVisualSettings(color, icon);
        ValidateGuid(creatorId, nameof(creatorId));

        var joinCode = GenerateRandomCode();
        var workspace = new ProjectWorkspace(Guid.NewGuid(), name, description, joinCode, color, icon, creatorId, visibility);
        
        // Creator becomes member with admin role
        workspace._members.Add(UserProjectWorkspace.Create(creatorId, workspace.Id, Role.Admin));
        
        // Add default statuses
        workspace.CreateDefaultStatuses();
        
        workspace.AddDomainEvent(new WorkspaceCreatedEvent(workspace.Id, workspace.Name, workspace.CreatorId));
        return workspace;
    }

    // === SELF MANAGEMENT METHODS ===
    
    public void UpdateBasicInfo(string name, string? description)
    {
        // Normalize inputs first
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        
        // Check for changes first
        if (Name == name && Description == description) return;
        
        // Then validate
        ValidateBasicInfo(name, description);

        var oldName = Name;
        var oldDescription = Description;
        Name = name;
        Description = description;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceBasicInfoUpdatedEvent(Id, oldName, name, oldDescription, description));
    }

    public void UpdateVisualSettings(string color, string icon)
    {
        // Normalize inputs first
        color = color?.Trim() ?? string.Empty;
        icon = icon?.Trim() ?? string.Empty;
        
        // Check for changes first
        if (Color == color && Icon == icon) return;
        
        // Then validate
        ValidateVisualSettings(color, icon);
        Color = color;
        Icon = icon;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceVisualSettingsUpdatedEvent(Id, color, icon));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        // Check for changes first
        if (Visibility == newVisibility) return;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceVisibilityChangedEvent(Id, newVisibility));
        
        // CASCADE: Update all child spaces to match workspace visibility if they were public
        CascadeVisibilityToSpaces(newVisibility);
    }

    public void Archive()
    {
        if (IsArchived) return;
        
        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceArchivedEvent(Id));
        
        // CASCADE: Archive all child spaces
        ArchiveAllSpaces();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        
        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceUnarchivedEvent(Id));
        
        // CASCADE: Unarchive all child spaces
        UnarchiveAllSpaces();
    }

    public void RegenerateJoinCode()
    {
        var oldJoinCode = JoinCode;
        JoinCode = GenerateRandomCode();
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceJoinCodeChangedEvent(Id, oldJoinCode, JoinCode));
    }

    // === MEMBERSHIP MANAGEMENT ===
    
    public void AddMember(Guid userId, Role role)
    {
        ValidateGuid(userId, nameof(userId));
        
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this workspace.");

        var member = UserProjectWorkspace.Create(userId, Id, role);
        _members.Add(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToWorkspaceEvent(Id, userId, role));
        
        // CASCADE: Add member to all private child spaces
        CascadeMembershipToSpaces(userId, isAdding: true);
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId)
            throw new InvalidOperationException("Cannot remove workspace creator.");
            
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this workspace.");

        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromWorkspaceEvent(Id, userId));
        
        // CASCADE: Remove member from all child spaces
        CascadeMembershipToSpaces(userId, isAdding: false);
    }

    public void ChangeMemberRole(Guid userId, Role newRole)
    {
        if (userId == CreatorId && newRole != Role.Admin)
            throw new InvalidOperationException("Workspace creator must remain admin.");
            
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this workspace.");

        // Check for changes first
        if (member.Role == newRole) return;
        
        member.UpdateRole(newRole);
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(Id, userId, newRole));
    }

    public void TransferOwnership(Guid newOwnerId)
    {
        var newOwner = _members.FirstOrDefault(m => m.UserId == newOwnerId);
        if (newOwner == null)
            throw new InvalidOperationException("New owner must be a workspace member.");

        var oldOwnerId = CreatorId;
        CreatorId = newOwnerId;
        
        // Ensure new owner has admin role
        if (newOwner.Role != Role.Owner)
            newOwner.UpdateRole(Role.Owner);
        
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceOwnershipTransferredEvent(Id, oldOwnerId, newOwnerId));
    }

    // === WORKFLOW STATUS MANAGEMENT ===
    
    public Status CreateStatus(string name, string color, bool isDefaultStatus = false)
    {
        // Normalize inputs
        name = name?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;
        
        // Validate
        ValidateStatusCreation(name, color, isDefaultStatus);

        var orderIndex = _statuses.Count;
        var status = Status.Create(name, color, orderIndex, Id);
        _statuses.Add(status);
        
        UpdateTimestamp();
        AddDomainEvent(new StatusCreatedInWorkspaceEvent(Id, status.Id, name));
        return status;
    }

    public void UpdateStatus(Guid statusId, string name, string color)
    {
        var status = _statuses.FirstOrDefault(s => s.Id == statusId);
        if (status == null)
            throw new InvalidOperationException("Status not found in this workspace.");

        // Normalize inputs
        name = name?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;
        
        // Check for changes first
        if (status.Name == name && status.Color == color) return;
        
        // Then validate
        ValidateStatusUpdate(statusId, name, color);

        var oldName = status.Name;
        var oldColor = status.Color;
        status.UpdateDetails(name, color);
        
        UpdateTimestamp();
        AddDomainEvent(new StatusUpdatedInWorkspaceEvent(Id, statusId, oldName, name, oldColor, color));
    }

    public void DeleteStatus(Guid statusId)
    {
        var status = _statuses.FirstOrDefault(s => s.Id == statusId);
        if (status == null)
            throw new InvalidOperationException("Status not found in this workspace.");

        if (status.IsDefaultStatus)
            throw new InvalidOperationException("Cannot delete the default status.");

        // TODO: Check if status is being used by any tasks
        // This would typically involve a domain service or repository check

        _statuses.Remove(status);
        UpdateTimestamp();
        AddDomainEvent(new StatusDeletedFromWorkspaceEvent(Id, statusId, status.Name));
    }

    public void ReorderStatuses(List<Guid> statusIds)
    {
        if (statusIds.Count != _statuses.Count)
            throw new ArgumentException("Must provide all status IDs for reordering.", nameof(statusIds));

        var allStatusesExist = statusIds.All(id => _statuses.Any(s => s.Id == id));
        if (!allStatusesExist)
            throw new ArgumentException("One or more status IDs are invalid.", nameof(statusIds));

        for (int i = 0; i < statusIds.Count; i++)
        {
            var status = _statuses.First(s => s.Id == statusIds[i]);
            status.UpdateOrderIndex(i);
        }

        UpdateTimestamp();
        AddDomainEvent(new StatusesReorderedInWorkspaceEvent(Id, statusIds));
    }

    // === CHILD ENTITY MANAGEMENT (SPACES) ===
    
    public ProjectSpace CreateSpace(string name, string? description, string icon, string color, Visibility visibility)
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot create spaces in an archived workspace.");

        // Normalize inputs
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        icon = icon?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;
        
        // Validate
        ValidateSpaceCreation(name, icon, color);

        if (_spaces.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A space with the name '{name}' already exists.");

        var orderIndex = _spaces.Count;
        var space = new ProjectSpace(Guid.NewGuid(), Id, name, description, icon, color, visibility, orderIndex, CreatorId);
        _spaces.Add(space);
        
        UpdateTimestamp();
        AddDomainEvent(new SpaceCreatedInWorkspaceEvent(Id, space.Id, name, CreatorId));
        return space;
    }

    public void RemoveSpace(Guid spaceId)
    {
        var space = _spaces.FirstOrDefault(s => s.Id == spaceId);
        if (space == null)
            throw new InvalidOperationException("Space not found in this workspace.");

        // TODO: Check if space has folders/lists/tasks
        // This might require a domain service or repository check

        _spaces.Remove(space);
        UpdateTimestamp();
        AddDomainEvent(new SpaceRemovedFromWorkspaceEvent(Id, spaceId, space.Name));
    }

    public void ReorderSpaces(List<Guid> spaceIds)
    {
        if (spaceIds.Count != _spaces.Count)
            throw new ArgumentException("Must provide all space IDs for reordering.", nameof(spaceIds));

        var allSpacesExist = spaceIds.All(id => _spaces.Any(s => s.Id == id));
        if (!allSpacesExist)
            throw new ArgumentException("One or more space IDs are invalid.", nameof(spaceIds));

        for (int i = 0; i < spaceIds.Count; i++)
        {
            var space = _spaces.First(s => s.Id == spaceIds[i]);
            space.UpdateOrderIndex(i);
        }

        UpdateTimestamp();
        AddDomainEvent(new SpacesReorderedInWorkspaceEvent(Id, spaceIds));
    }

    // === BULK OPERATIONS (MISSING HIERARCHICAL METHODS) ===
    
    public void ArchiveAllSpaces()
    {
        var spacesToArchive = _spaces.Where(s => !s.IsArchived).ToList();
        if (!spacesToArchive.Any()) return;
        
        foreach (var space in spacesToArchive)
        {
            space.Archive();
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllSpacesArchivedInWorkspaceEvent(Id));
    }
    
    public void UnarchiveAllSpaces()
    {
        var spacesToUnarchive = _spaces.Where(s => s.IsArchived).ToList();
        if (!spacesToUnarchive.Any()) return;
        
        foreach (var space in spacesToUnarchive)
        {
            space.Unarchive();
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllSpacesUnarchivedInWorkspaceEvent(Id));
    }
    

    // === PRIVATE CASCADE METHODS ===
    
    private void CascadeVisibilityToSpaces(Visibility newVisibility)
    {
        // Only cascade if making more restrictive (Public -> Private)
        if (newVisibility == Visibility.Private)
        {
            foreach (var space in _spaces.Where(s => s.Visibility == Visibility.Public))
            {
                space.ChangeVisibility(Visibility.Private);
            }
        }
    }
    
    private void CascadeMembershipToSpaces(Guid userId, bool isAdding)
    {
        foreach (var space in _spaces.Where(s => s.Visibility == Visibility.Private))
        {
            try
            {
                if (isAdding)
                    space.AddMember(userId);
                else
                    space.RemoveMember(userId);
            }
            catch (InvalidOperationException)
            {
                // Member might already exist or not exist - ignore
            }
        }
    }

    // === PRIVATE HELPER METHODS ===
    
    private void CreateDefaultStatuses()
    {
        // Create standard workflow statuses
        var todoStatus = CreateStatus("To Do", "#6B7280", true); // Default status
        CreateStatus("In Progress", "#3B82F6");
        CreateStatus("Review", "#F59E0B");
        CreateStatus("Done", "#10B981"); // Completed status
    }

    private static string GenerateRandomCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    // === VALIDATION HELPER METHODS ===
    
    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Workspace name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500)
            throw new ArgumentException("Workspace description cannot exceed 500 characters.", nameof(description));
    }
    
    private static void ValidateVisualSettings(string color, string icon)
    {
        ValidateColor(color);
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Workspace icon cannot be empty.", nameof(icon));
    }
    
    private static void ValidateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color cannot be empty.", nameof(color));
        if (!IsValidColorCode(color))
            throw new ArgumentException("Invalid color format.", nameof(color));
    }
    
    private void ValidateStatusCreation(string name, string color, bool isDefaultStatus)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Status color cannot be empty.", nameof(color));

        if (_statuses.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A status with the name '{name}' already exists.");

        if (isDefaultStatus && _statuses.Any(s => s.IsDefaultStatus))
            throw new InvalidOperationException("Only one default status is allowed per workspace.");
    }
    
    private void ValidateStatusUpdate(Guid statusId, string name, string color)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Status color cannot be empty.", nameof(color));

        // Check for name conflicts (excluding current status)
        if (_statuses.Any(s => s.Id != statusId && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A status with the name '{name}' already exists.");
    }
    
    private static void ValidateSpaceCreation(string name, string icon, string color)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Space name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Space icon cannot be empty.", nameof(icon));
        ValidateColor(color);
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
    
    public Role? GetMemberRole(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId)?.Role;
    
    public Status? GetDefaultStatus() => _statuses.FirstOrDefault(s => s.IsDefaultStatus);
    
    
    public bool CanUserManage(Guid userId) => 
        userId == CreatorId || GetMemberRole(userId) == Role.Admin;
        
    public bool CanUserEdit(Guid userId) => 
        CanUserManage(userId) || GetMemberRole(userId) == Role.Member;
        
    public int GetTotalSpaceCount() => _spaces.Count;
    
    public int GetTotalFolderCount() => _spaces.Sum(s => s.GetFolderCount());
    
    public int GetTotalListCount() => _spaces.Sum(s => s.GetTotalListCount());
    
    public int GetTotalTaskCount() => _spaces.Sum(s => s.GetTotalTaskCount());
}