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
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Workspace color cannot be empty.", nameof(color));
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Workspace icon cannot be empty.", nameof(icon));

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
    
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Workspace name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceNameUpdatedEvent(Id, newName));
    }

    public void UpdateDescription(string? newDescription)
    {
        if (Description == newDescription || string.IsNullOrWhiteSpace(newDescription)) return;
       
        Description = newDescription;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceDescriptionUpdatedEvent(Id, newDescription));
    }

    public void UpdateVisualSettings(string color, string icon)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Workspace color cannot be empty.", nameof(color));
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Workspace icon cannot be empty.", nameof(icon));

        var changed = Color != color || Icon != icon;
        if (!changed) return;

        Color = color;
        Icon = icon;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceVisualSettingsUpdatedEvent(Id, color, icon));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceVisibilityChangedEvent(Id, newVisibility));
    }

    public void Archive()
    {
        if (IsArchived) return;
        
        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceArchivedEvent(Id));
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        
        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceUnarchivedEvent(Id));
    }

    public void RegenerateJoinCode()
    {
        var oldJoinCode = JoinCode;
        JoinCode = GenerateRandomCode();
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceJoinCodeChangedEvent(Id, JoinCode, oldJoinCode));
    }

    // === MEMBERSHIP MANAGEMENT ===
    
    public void AddMember(Guid userId, Role role)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this workspace.");

        var member = UserProjectWorkspace.Create(userId, Id, role);
        _members.Add(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToWorkspaceEvent(Id, userId, role));
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
    }

    public void ChangeMemberRole(Guid userId, Role newRole)
    {
        if (userId == CreatorId && newRole != Role.Admin)
            throw new InvalidOperationException("Workspace creator must remain admin.");
            
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this workspace.");

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
        if (newOwner.Role != Role.Admin)
            newOwner.UpdateRole(Role.Admin);
        
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceOwnershipTransferredEvent(Id, oldOwnerId, newOwnerId));
    }

    // === WORKFLOW STATUS MANAGEMENT ===
    
    public Status CreateStatus(string name, string color, bool isDefaultStatus = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Status color cannot be empty.", nameof(color));

        if (_statuses.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A status with the name '{name}' already exists.");

        if (isDefaultStatus && _statuses.Any(s => s.IsDefaultStatus))
            throw new InvalidOperationException("Only one default status is allowed per workspace.");

        var orderIndex = _statuses.Count;
        var status = Status.Create(name, color, orderIndex,Id);
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

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Status color cannot be empty.", nameof(color));

        // Check for name conflicts (excluding current status)
        if (_statuses.Any(s => s.Id != statusId && s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A status with the name '{name}' already exists.");

        var changed = status.Name != name || status.Color != color;
        if (!changed) return;

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
            throw new ArgumentException("Must provide all status IDs for reordering.");

        var allStatusesExist = statusIds.All(id => _statuses.Any(s => s.Id == id));
        if (!allStatusesExist)
            throw new ArgumentException("One or more status IDs are invalid.");

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

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Space name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Space icon cannot be empty.", nameof(icon));
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Space color cannot be empty.", nameof(color));

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
            throw new ArgumentException("Must provide all space IDs for reordering.");

        var allSpacesExist = spaceIds.All(id => _spaces.Any(s => s.Id == id));
        if (!allSpacesExist)
            throw new ArgumentException("One or more space IDs are invalid.");

        for (int i = 0; i < spaceIds.Count; i++)
        {
            var space = _spaces.First(s => s.Id == spaceIds[i]);
            space.UpdateOrderIndex(i);
        }

        UpdateTimestamp();
        AddDomainEvent(new SpacesReorderedInWorkspaceEvent(Id, spaceIds));
    }

    // === PRIVATE HELPER METHODS ===
    
    private void CreateDefaultStatuses()
    {
        // Create standard workflow statuses
        var todoStatus = CreateStatus("To Do", "#6B7280", true); // Default status
        CreateStatus("In Progress", "#3B82F6", true);
        CreateStatus("Review", "#F59E0B", true);
        CreateStatus("Done", "#10B981", true); // Completed status
    }

    private static string GenerateRandomCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    // === QUERY HELPER METHODS ===
    
    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);
    
    public Role? GetMemberRole(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId)?.Role;
    
    public Status? GetDefaultStatus() => _statuses.FirstOrDefault(s => s.IsDefaultStatus);
    
    public bool CanUserManage(Guid userId) => 
        userId == CreatorId || GetMemberRole(userId) == Role.Admin;
        
    public bool CanUserEdit(Guid userId) => 
        CanUserManage(userId) || GetMemberRole(userId) == Role.Member;
}