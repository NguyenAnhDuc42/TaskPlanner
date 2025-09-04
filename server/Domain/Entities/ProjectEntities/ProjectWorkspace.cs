
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using Domain.Services.UsageChecker;
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
        workspace._members.Add(UserProjectWorkspace.Create(creatorId, workspace.Id, Role.Admin));

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

    public Status CreateStatus(string name, string color, bool isDefaultStatus = false)
    {
        name = name?.Trim() ?? string.Empty;
        color = color?.Trim() ?? string.Empty;

        ValidateStatusCreation(name, color, isDefaultStatus);

        var orderIndex = _statuses.Count;
        var status = Status.Create(name, color, orderIndex, Id);
        _statuses.Add(status);

        UpdateTimestamp();
        return status;
    }

    public void UpdateStatus(Guid statusId, string name, string color, bool? isDefaultStatus = null)
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
        if (isDefaultStatus.HasValue && isDefaultStatus.Value != status.IsDefaultStatus)
        {
            if (isDefaultStatus.Value)
            {
                status.SetDefault(isDefaultStatus.Value);
            }
        }
        status.UpdateDetails(name, color);


        UpdateTimestamp();
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