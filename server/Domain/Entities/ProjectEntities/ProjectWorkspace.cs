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
using System.Threading.Tasks;

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

    private readonly List<Status> _statuses = new();
    public IReadOnlyCollection<Status> Statuses => _statuses.AsReadOnly();

    private readonly List<Tag> _tags = new();
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    
    private readonly List<UserProjectWorkspace> _members = new();
    public IReadOnlyCollection<UserProjectWorkspace> Members => _members.AsReadOnly();

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
        // Normalize inputs
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

    public void DeleteStatus(Guid statusId)
    {
        throw new InvalidOperationException("This operation requires a domain-service check. Use DeleteStatus(Guid statusId, IStatusUsageChecker usageChecker).");
    }

    public async Task DeleteStatus(Guid statusId, IStatusUsageCheker usageChecker)
    {
        if (usageChecker == null) throw new ArgumentNullException(nameof(usageChecker));

        var status = _statuses.FirstOrDefault(s => s.Id == statusId);
        if (status == null)
            throw new InvalidOperationException("Status not found in this workspace.");

        if (status.IsDefaultStatus)
            throw new InvalidOperationException("Cannot delete the default status.");

        if (await usageChecker.IsInUseAsync(statusId))
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

    // === PRIVATE HELPERS ===

    private void CreateDefaultStatuses()
    {
        CreateStatus("To Do", "#6B7280", true);
        CreateStatus("In Progress", "#3B82F6");
        CreateStatus("Review", "#F59E0B");
        CreateStatus("Done", "#10B981");
    }

    private static string GenerateRandomCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

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

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}
