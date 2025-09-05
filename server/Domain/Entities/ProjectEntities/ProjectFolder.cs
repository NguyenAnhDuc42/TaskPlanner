using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;

namespace Domain.Entities.ProjectEntities;

public class ProjectFolder : Aggregate, IHasWorkspaceId
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public long? OrderKey { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }
    public Guid WorkspaceId => ProjectWorkspaceId;

    private readonly List<UserProjectFolder> _members = new();
    public IReadOnlyCollection<UserProjectFolder> Members => _members.AsReadOnly();

    private ProjectFolder() { } // For EF Core

    internal ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name,
        string? description, Visibility visibility, long orderKey, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Description = description;
        Visibility = visibility;
        OrderKey = orderKey;
        CreatorId = creatorId;
    }

    // === VALIDATION METHOD ===
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

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
    }

    internal void UpdateOrderKey(long newOrderKey)
    {
        if (newOrderKey < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrderKey), "Order key cannot be negative.");

        if (OrderKey == newOrderKey) return;

        OrderKey = newOrderKey;
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

    // === VALIDATION HELPERS ===
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

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}