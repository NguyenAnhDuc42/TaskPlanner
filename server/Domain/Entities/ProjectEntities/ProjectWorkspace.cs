using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using Domain.Events.WorkspaceEvents;
using Domain.Services.UsageChecker;
using static Domain.Common.ColorValidator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectWorkspace : Aggregate
{
    private const int MAX_BATCH_SIZE = 1000; // Safety limit for BATCH_RULE operations

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string JoinCode { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public string Icon { get; private set; } = null!;
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }

    private readonly List<Status> _statuses = new();
    public IReadOnlyCollection<Status> Statuses => _statuses.AsReadOnly();
    private readonly List<Tag> _tags = new();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    
    private readonly List<UserProjectWorkspace> _members = new();
    public IReadOnlyCollection<UserProjectWorkspace> Members => _members.AsReadOnly();

    private readonly List<ProjectSpace> _spaces = new();
    public IReadOnlyCollection<ProjectSpace> Spaces => _spaces.AsReadOnly();

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

    public static ProjectWorkspace Create(string name, string? description, string color, string icon, Guid creatorId, Visibility visibility)
    {
        // Normalize inputs first
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        color = color?.Trim() ?? string.Empty;
        icon = icon?.Trim() ?? string.Empty;

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

    // === SELF MANAGEMENT ===

    public void UpdateBasicInfo(string name, string? description)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (Name == name && Description == description) return;

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
        color = color?.Trim() ?? string.Empty;
        icon = icon?.Trim() ?? string.Empty;

        if (Color == color && Icon == icon) return;

        ValidateVisualSettings(color, icon);
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

        // CASCADE: Update all child spaces to match workspace visibility if they were public
        CascadeVisibilityToSpaces(newVisibility);
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceArchivedEvent(Id));

        // BATCH_RULE: archive immediate children (spaces). Safety limit enforced.
        ArchiveAllSpaces();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new WorkspaceUnarchivedEvent(Id));

        // BATCH_RULE: unarchive children
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

        // CASCADE: Add member to all private child spaces (best-effort via pre-checks)
        foreach (var space in _spaces.Where(s => s.Visibility == Visibility.Private))
        {
            // Only call when not present to avoid exceptions
            if (!space.Members.Any(m => m.UserId == userId))
                space.AddMember(userId);
        }
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

        // Cascade: Remove member from all child spaces
        foreach (var space in _spaces.Where(s => s.Visibility == Visibility.Private))
        {
            var exists = space.Members.Any(m => m.UserId == userId);
            if (exists)
                space.RemoveMember(userId);
        }
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

        if (newOwner.Role != Role.Owner)
            newOwner.UpdateRole(Role.Owner);

        UpdateTimestamp();
        AddDomainEvent(new WorkspaceOwnershipTransferredEvent(Id, oldOwnerId, newOwnerId));
    }

    // === WORKFLOW STATUS MANAGEMENT ===

    public Status CreateStatus(string name, string color, bool isDefaultStatus = false)
    {
        name = name?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;

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

        name = name?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;

        if (status.Name == name && status.Color == color) return;

        ValidateStatusUpdate(statusId, name, color);

        var oldName = status.Name;
        var oldColor = status.Color;
        status.UpdateDetails(name, color);

        UpdateTimestamp();
        AddDomainEvent(new StatusUpdatedInWorkspaceEvent(Id, statusId, oldName, name, oldColor, color));
    }

    /// <summary>
    /// Delete a status. This operation must ensure the status is not in use by tasks.
    /// Prefer calling the overload that accepts IStatusUsageChecker.
    /// </summary>
    public void DeleteStatus(Guid statusId)
    {
        throw new InvalidOperationException("This operation requires a domain-service check. Use DeleteStatus(Guid statusId, IStatusUsageChecker usageChecker).");
    }

    /// <summary>
    /// Delete a status. Caller must provide an IStatusUsageChecker to verify the status is not in use.
    /// </summary>
    public async Task DeleteStatus(Guid statusId, IStatusUsageCheker usageChecker)
    {
        if (usageChecker == null) throw new ArgumentNullException(nameof(usageChecker));

        var status = _statuses.FirstOrDefault(s => s.Id == statusId);
        if (status == null)
            throw new InvalidOperationException("Status not found in this workspace.");

        if (status.IsDefaultStatus)
            throw new InvalidOperationException("Cannot delete the default status.");

        if ( await usageChecker.IsInUseAsync(statusId))
            throw new InvalidOperationException("Cannot delete a status that is currently in use by tasks.");

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

        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        icon = icon?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;

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

    /// <summary>
    /// Remove a space. This overload requires a domain service to check if the space has children that prevent removal.
    /// </summary>
    public void RemoveSpace(Guid spaceId)
    {
        throw new InvalidOperationException("This operation requires a domain-service check. Use RemoveSpace(Guid spaceId, ISpaceUsageChecker usageChecker).");
    }

    /// <summary>
    /// Remove a space. Caller must provide ISpaceUsageChecker to verify space is safe to remove.
    /// </summary>
    public async Task RemoveSpace(Guid spaceId, ISpaceUsageChecker usageChecker)
    {
        if (usageChecker == null) throw new ArgumentNullException(nameof(usageChecker));

        var space = _spaces.FirstOrDefault(s => s.Id == spaceId);
        if (space == null)
            throw new InvalidOperationException("Space not found in this workspace.");

        if ( await usageChecker.IsInUseAsync(spaceId))
            throw new InvalidOperationException("Cannot remove space because it contains folders/lists/tasks according to the provided checker.");

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

    // === BULK OPERATIONS (BATCH_RULE with safety) ===

    private void ArchiveAllSpaces()
    {
        var spacesToArchive = _spaces.Where(s => !s.IsArchived).ToList();
        if (!spacesToArchive.Any()) return;

        if (spacesToArchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

        foreach (var space in spacesToArchive)
        {
            space.Archive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllSpacesArchivedInWorkspaceEvent(Id));
    }

    private void UnarchiveAllSpaces()
    {
        var spacesToUnarchive = _spaces.Where(s => s.IsArchived).ToList();
        if (!spacesToUnarchive.Any()) return;

        if (spacesToUnarchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

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
        if (newVisibility == Visibility.Private)
        {
            foreach (var space in _spaces.Where(s => s.Visibility == Visibility.Public))
            {
                space.ChangeVisibility(Visibility.Private);
            }
        }
    }

    // === PRIVATE HELPERS ===

    private void CreateDefaultStatuses()
    {
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

    // === VALIDATION HELPERS ===

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
        if (isDefaultStatus && _statuses.Any(s => s.IsDefaultStatus))
            throw new InvalidOperationException("A default status already exists in this workspace.");
    }

    private void ValidateStatusUpdate(Guid statusId, string newName, string newColor)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Status name cannot be empty.", nameof(newName));
        if (string.IsNullOrWhiteSpace(newColor))
            throw new ArgumentException("Status color cannot be empty.", nameof(newColor));
    }

    private static void ValidateSpaceCreation(string name, string icon, string color)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Space name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(icon))
            throw new ArgumentException("Space icon cannot be empty.", nameof(icon));
        ValidateColor(color);
    }

    // Reuse base helper
    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}
