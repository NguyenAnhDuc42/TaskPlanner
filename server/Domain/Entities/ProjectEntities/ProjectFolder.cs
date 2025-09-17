using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.ProjectEntities;

public class ProjectFolder : Entity
{
    [Required] public Guid ProjectSpaceId { get; private set; }
    [Required] public string Name { get; private set; } = null!;
    public string Color { get; private set; } = "#cdcbcbff";
    public long? OrderKey { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    [Required] public Guid CreatorId { get; private set; }
    private ProjectFolder() { } // For EF Core

    internal ProjectFolder(Guid id, Guid projectSpaceId, string name, Visibility visibility, long orderKey, Guid creatorId)
    {
        Id = id;
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Visibility = visibility;
        OrderKey = orderKey;
        CreatorId = creatorId;
    }

    // === VALIDATION METHOD ===
    public static void ValidateForCreation(string name, string? description, string color,
        Guid projectSpaceId, Guid creatorId)
    {
        ValidateBasicInfo(name, description);
        ValidateColor(color);
        ValidateGuid(projectSpaceId, nameof(projectSpaceId));
        ValidateGuid(creatorId, nameof(creatorId));
    }

    // === SELF MANAGEMENT METHODS ===
    public void UpdateBasicInfo(string name, string? description)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (Name == name ) return;

        ValidateBasicInfo(name, description);

        var oldName = Name;
        Name = name;
     
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