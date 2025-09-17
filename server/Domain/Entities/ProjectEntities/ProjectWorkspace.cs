
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;
using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace Domain.Entities.ProjectEntities;

public class ProjectWorkspace : Entity
{
    [Required] public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    [Required]public string JoinCode { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public string Icon { get; private set; } = null!;
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    [Required]public Guid CreatorId { get; private set; }

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


    public static ProjectWorkspace Create(string name, string? description,string joinCode, string color, string icon, Guid creatorId, Visibility visibility)
    {
        // Normalize inputs
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        color = color?.Trim() ?? string.Empty;
        icon = icon?.Trim() ?? string.Empty;

        ValidateBasicInfo(name, description);
        ValidateVisualSettings(color, icon);

        var workspace = new ProjectWorkspace(Guid.NewGuid(), name, description, joinCode, color, icon, creatorId, visibility);

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

}