using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;
using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Enums.Workspace;

namespace Domain.Entities.ProjectEntities;

public sealed class ProjectWorkspace : Entity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string JoinCode { get; private set; } = null!;
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public Theme Theme { get; private set; } = Theme.System;
    public WorkspaceVariant Variant { get; private set; } = WorkspaceVariant.Team;
    public bool StrictJoin { get; private set; } = false;
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }

    // EF Core
    private ProjectWorkspace() { }

    private ProjectWorkspace(Guid id, string name, string? description, string joinCode, Customization customization, Theme theme, WorkspaceVariant variant, bool strictJoin, Guid creatorId)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        JoinCode = joinCode ?? throw new ArgumentNullException(nameof(joinCode));
        Customization = customization ?? Customization.CreateDefault();
        Theme = theme;
        Variant = variant;
        StrictJoin = strictJoin;
        CreatorId = creatorId;
        IsArchived = false;
    }

    public static ProjectWorkspace Create(string name, string? description, string? joinCode, Customization? customization, Guid creatorId, Theme theme = Theme.System, WorkspaceVariant variant = WorkspaceVariant.Team, bool strictJoin = false)
        => new ProjectWorkspace(Guid.NewGuid(), name?.Trim() ?? throw new ArgumentNullException(nameof(name)), string.IsNullOrWhiteSpace(description) ? null : description?.Trim(), string.IsNullOrWhiteSpace(joinCode) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : joinCode.Trim(), customization ?? Customization.CreateDefault(), theme, variant, strictJoin, creatorId);

    // Consolidated update — single method for partial updates (visuals, name/description, theme/variant/strictJoin, optionally regenerate join code)
    public void Update(string? name = null, string? description = null, string? color = null, string? icon = null, Theme? theme = null, WorkspaceVariant? variant = null, bool? strictJoin = null, bool? isArchived = null, bool regenerateJoinCode = false)
    {
        var changed = false;

        // Prepare candidate values (null means "no-change")
        var candidateName = name is null ? Name : (name.Trim() == string.Empty ? throw new ArgumentException("Name cannot be empty.", nameof(name)) : name.Trim());
        var candidateDescription = description is null ? Description : (string.IsNullOrWhiteSpace(description.Trim()) ? null : description.Trim());

        // Validate basics against final candidates
        ValidateBasicInfo(candidateName, candidateDescription);

        if (candidateName != Name) { Name = candidateName; changed = true; }
        if (candidateDescription != Description) { Description = candidateDescription; changed = true; }

        // visuals (replace VO as a whole)
        if (color is not null || icon is not null)
        {
            var c = color?.Trim() ?? Customization.Color;
            var i = icon?.Trim() ?? Customization.Icon;
            var newCustomization = Customization.Create(c, i);
            if (!newCustomization.Equals(Customization)) { Customization = newCustomization; changed = true; }
        }

        if (theme.HasValue && theme.Value != Theme) { Theme = theme.Value; changed = true; }
        if (variant.HasValue && variant.Value != Variant) { Variant = variant.Value; changed = true; }
        if (strictJoin.HasValue && strictJoin.Value != StrictJoin) { StrictJoin = strictJoin.Value; changed = true; }

        if (isArchived.HasValue && isArchived.Value != IsArchived) { IsArchived = isArchived.Value; changed = true; }

        if (regenerateJoinCode)
        {
            var newJoin = GenerateRandomCode();
            if (newJoin != JoinCode) { JoinCode = newJoin; changed = true; }
        }

        if (changed) UpdateTimestamp();
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }
    public void RegenerateJoinCode() { JoinCode = GenerateRandomCode(); UpdateTimestamp(); }

    // --- helpers & validations ---
    private static string GenerateRandomCode(int length = 6) { const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray()); }
    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Workspace name cannot be empty.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("Workspace name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500) throw new ArgumentException("Workspace description cannot exceed 500 characters.", nameof(description));
    }
}
