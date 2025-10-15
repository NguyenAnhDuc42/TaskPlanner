using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.ProjectEntities.ValueObject;

namespace Domain.Entities.ProjectEntities;

public class ProjectFolder : Entity
{
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public long? OrderKey { get; private set; }
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }

    // EF Core
    private ProjectFolder() { }

    private ProjectFolder(Guid id, Guid spaceId, string name, Customization customization, bool isPrivate, long? orderKey, Guid creatorId)
    {
        Id = id;
        ProjectSpaceId = spaceId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Customization = customization ?? Customization.CreateDefault();
        IsPrivate = isPrivate;
        OrderKey = orderKey;
        CreatorId = creatorId;
        IsArchived = false;
    }

    public static ProjectFolder Create(Guid spaceId, string name, Customization? customization, bool isPrivate, Guid creatorId, long? orderKey = null)
        => new ProjectFolder(Guid.NewGuid(), spaceId, name?.Trim() ?? throw new ArgumentNullException(nameof(name)), customization ?? Customization.CreateDefault(), isPrivate, orderKey, creatorId);

    // Consolidated update: name/visuals/privacy/order/archive
    public void Update(string? name = null, string? description = null, string? color = null, string? icon = null, bool? isPrivate = null, long? orderKey = null, bool? isArchived = null)
    {
        var changed = false;

        if (name is not null)
        {
            var n = name.Trim();
            if (string.IsNullOrWhiteSpace(n)) throw new ArgumentException("Name cannot be empty.", nameof(name));
            if (n != Name) { Name = n; changed = true; }
        }

        if (color is not null || icon is not null)
        {
            var c = color?.Trim() ?? Customization.Color;
            var i = icon?.Trim() ?? Customization.Icon;
            var newCustomization = Customization.Create(c, i);
            if (!newCustomization.Equals(Customization)) { Customization = newCustomization; changed = true; }
        }

        if (isPrivate.HasValue && isPrivate.Value != IsPrivate) { IsPrivate = isPrivate.Value; changed = true; }
        if (orderKey.HasValue && orderKey != OrderKey) { if (orderKey < 0) throw new ArgumentOutOfRangeException(nameof(orderKey), "Order key cannot be negative."); OrderKey = orderKey; changed = true; }
        if (isArchived.HasValue && isArchived.Value != IsArchived) { IsArchived = isArchived.Value; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public static void ValidateForCreation(string name, string? description, string color, Guid projectSpaceId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("Name too long.", nameof(name));
        if (description?.Length > 500) throw new ArgumentException("Description too long.", nameof(description));
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color required.", nameof(color));
        if (!IsValidColorCode(color)) throw new ArgumentException("Invalid color.", nameof(color));
        if (projectSpaceId == Guid.Empty) throw new ArgumentException("projectSpaceId required.", nameof(projectSpaceId));
        if (creatorId == Guid.Empty) throw new ArgumentException("creatorId required.", nameof(creatorId));
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }
}
