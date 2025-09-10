
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;

namespace Domain.Entities.ProjectEntities;

public class ProjectWorkspace : Aggregate, IHasWorkspaceId
{
    public Guid ProjectWorkspaceId => Id;
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
        workspace._members.Add(UserProjectWorkspace.Create(creatorId, workspace.Id, Role.Owner));

        // Add default statuses
        workspace.CreateDefaultStatuses();

        return workspace;
    }

    // === SELF MANAGEMENT ===
    public void Update(string? name, string? description, string? color, string? icon, Visibility? visibility, bool? isArchived, bool regenerateJoinCode = false)
    {
        bool changed = false;
        string? newJoinCode = null;
        ValidateBasicInfo(name ?? Name, description ?? Description);
        ValidateVisualSettings(color ?? Color, icon ?? Icon);

        if (name != null && Name != name)
        {
            Name = name;
            changed = true;
        }

        if (description != null && Description != description)
        {
            Description = description;
            changed = true;
        }

        if (color != null && Color != color)
        {
            Color = color;
            changed = true;
        }

        if (icon != null && Icon != icon)
        {
            Icon = icon;
            changed = true;
        }

        if (visibility.HasValue && Visibility != visibility.Value)
        {
            if (visibility.Value != Visibility) { Visibility = visibility.Value; changed = true; }
        }

        if (isArchived.HasValue && IsArchived != isArchived.Value)
        {
            if (isArchived.Value != IsArchived) { IsArchived = isArchived.Value; changed = true; }
        }
        if (regenerateJoinCode)
        {
            newJoinCode = GenerateRandomCode();
            if (newJoinCode != JoinCode) { JoinCode = newJoinCode; changed = true; }
        }

        if (changed)
        {
            UpdateTimestamp();
        }
    }

    public void UpdateBasicInfo(string name, string description)
    {
        name = name.Trim();
        description = description.Trim();

        if (string.IsNullOrWhiteSpace(description))
            description = string.Empty;

        if (Name == name && Description == description)
            return;

        ValidateBasicInfo(name, description);

        Name = name;
        Description = description;
        UpdateTimestamp();
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
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;
        Visibility = newVisibility;
        UpdateTimestamp();
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
    }

    public void RegenerateJoinCode()
    {
        var oldJoinCode = JoinCode;
        JoinCode = GenerateRandomCode();
        UpdateTimestamp();
    }

    // === MEMBERSHIP MANAGEMENT ===
    public void AddMember(Guid userId, Role role)
    {
        if (Members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException("User is already a member of this workspace.");
        }

        var userWorkspace = UserProjectWorkspace.Create(userId, Id, role);
        _members.Add(userWorkspace);
    }
    public void RemoveMember(Guid userId)
    {
        if (!Members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException("User is not a member of this workspace.");
        }
        if (CreatorId == userId)
        {
            throw new InvalidOperationException("Cannot remove workspace creator");
        }
        var memberToRemove = Members.First(m => m.UserId == userId);
        _members.Remove(memberToRemove);
    }
    public void AddPendingMember(Guid userId)
    {
        if (Members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException("User is already a member of this workspace.");
        }

        var userWorkspace = UserProjectWorkspace.Create(userId, Id, Role.Member, isPending: true);
        _members.Add(userWorkspace);
    }
    public void AddMembers(IEnumerable<Guid> userIds, Role role)
    {
        if (userIds == null || !userIds.Any()) return;

        var addedMembers = new List<UserProjectWorkspace>();

        foreach (var userId in userIds)
        {
            if (_members.Any(m => m.UserId == userId)) continue; // skip duplicates

            var member = UserProjectWorkspace.Create(userId, Id, role);
            _members.Add(member);
            addedMembers.Add(member);
        }

        if (addedMembers.Any())
        {
        }
    }

    public void RemoveMembers(IEnumerable<Guid> userIds)
    {
        if (userIds == null || !userIds.Any()) return;

        var removedMembers = new List<UserProjectWorkspace>();

        foreach (var userId in userIds)
        {
            var member = _members.FirstOrDefault(m => m.UserId == userId);
            if (member == null) continue; // skip non-members
            if (member.Role == Role.Owner) continue; // skip owner

            _members.Remove(member);
            removedMembers.Add(member);
        }

        if (removedMembers.Any())
        {
        }
    }

    public void ChangeMemberRoles(IEnumerable<Guid> userIds, Role newRole)
    {
        if (userIds == null || !userIds.Any()) return;

        var updatedMembers = new List<UserProjectWorkspace>();

        foreach (var userId in userIds)
        {
            var member = _members.FirstOrDefault(m => m.UserId == userId);
            if (member == null || member.Role == newRole) continue; // skip non-members or no-op
            if (member.Role == Role.Owner) continue; // skip owner

            member.UpdateRole(newRole);
            updatedMembers.Add(member);
        }

        if (updatedMembers.Any())
        {
        }
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
    }

    // === WORKFLOW STATUS MANAGEMENT ===

    public Status CreateStatus(string name, string color, long orderKey, bool isDefaultStatus = false)
    {
        name = name?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;

        ValidateStatusCreation(name, color, isDefaultStatus);

        var status = Status.Create(name, color, orderKey, Id);
        _statuses.Add(status);

        UpdateTimestamp();
        return status;
    }

    public void UpdateStatus(Guid statusId, string name, string color, long? orderKey = null, bool? isDefaultStatus = null)
    {
        var status = _statuses.FirstOrDefault(s => s.Id == statusId);
        if (status == null)
            throw new InvalidOperationException("Status not found in this workspace.");

        name = name?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;

        ValidateStatusUpdate(statusId, name, color);

        if (isDefaultStatus.HasValue && isDefaultStatus.Value != status.IsDefaultStatus)
        {
            status.SetDefault(isDefaultStatus.Value);
        }

        if (orderKey.HasValue)
        {
            status.UpdateOrderKey(orderKey.Value);
        }

        status.UpdateDetails(name, color);
        UpdateTimestamp();
    }

    public void RemoveStatus(Guid statusId)
    {
        var status = _statuses.FirstOrDefault(s => s.Id == statusId);
        if (status == null)
            throw new InvalidOperationException("Status not found in this workspace.");

        if (status.IsDefaultStatus)
            throw new InvalidOperationException("Default status cannot be removed.");

        _statuses.Remove(status);
        UpdateTimestamp();
    }


    // === WORKFLOW TAG MANAGEMENT ===
    public Tag AddTag(string name, string? color)
    {
        name = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));

        // enforce unique name within workspace (case-insensitive)
        if (_tags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Tag '{name}' already exists in this workspace.");

        var tag = Tag.Create(name, color, Id);
        _tags.Add(tag);

        UpdateTimestamp();
        return tag;
    }

    public void RemoveTag(Guid tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.Id == tagId);
        if (tag == null)
            throw new InvalidOperationException("Tag not found in this workspace.");

        _tags.Remove(tag);
        UpdateTimestamp();
    }


    // === PRIVATE HELPERS ===

    private void CreateDefaultStatuses()
    {
        CreateStatus("To Do", "#6B7280", 10000, true);
        CreateStatus("In Progress", "#3B82F6", 20000);
        CreateStatus("Review", "#F59E0B", 30000);
        CreateStatus("Done", "#10B981", 40000);
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