using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.ProjectEntities;

public sealed class ProjectFolder : Entity
{
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public long OrderKey { get; private set; }
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }
    public long NextListOrder { get; private set; } = 10_000_000L;

    private ProjectFolder() { }

    private ProjectFolder(Guid id, Guid projectSpaceId, string name, Customization customization, bool isPrivate, long orderKey, Guid creatorId)
    {
        Id = id;
        ProjectSpaceId = projectSpaceId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("Name too long.", nameof(name));
        Customization = customization ?? throw new ArgumentNullException(nameof(customization));
        IsPrivate = isPrivate;
        OrderKey = orderKey;
        CreatorId = creatorId;
        IsArchived = false;
    }

    public static ProjectFolder Create(Guid projectSpaceId, string name, string color, string icon, bool isPrivate, Guid creatorId, long orderKey)
    {
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color required.", nameof(color));
        if (!IsValidColorCode(color)) throw new ArgumentException("Invalid color.", nameof(color));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));

        var customization = Customization.Create(color.Trim(), icon.Trim());
        return new ProjectFolder(Guid.NewGuid(), projectSpaceId, name?.Trim() ?? throw new ArgumentNullException(nameof(name)), customization, isPrivate, orderKey, creatorId);
    }

    public long GetNextListOrderAndIncrement()
    {
        var currentOrder = NextListOrder;
        NextListOrder += 10_000_000L;
        return currentOrder;
    }

    public void Update(string? name = null, string? color = null, string? icon = null, bool? isPrivate = null, long? orderKey = null, bool? isArchived = null)
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
        if (orderKey.HasValue && orderKey != OrderKey) { if (orderKey < 0) throw new ArgumentOutOfRangeException(nameof(orderKey), "Order key cannot be negative."); OrderKey = orderKey.Value; changed = true; }
        if (isArchived.HasValue && isArchived.Value != IsArchived) { IsArchived = isArchived.Value; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public void UpdateDetails(string name, string? color = null, string? icon = null)
    {
        var changed = false;
        var n = name.Trim();
        if (string.IsNullOrWhiteSpace(n)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (n != Name)
        {
            Name = n;
            changed = true;
        }

        if (color is not null || icon is not null)
        {
            var c = color?.Trim() ?? Customization.Color;
            var i = icon?.Trim() ?? Customization.Icon;
            var newCustomization = Customization.Create(c, i);
            if (!newCustomization.Equals(Customization)) { Customization = newCustomization; changed = true; }
        }

        if (changed) UpdateTimestamp();
    }

    public void UpdatePrivacy(bool isPrivate)
    {
        if (IsPrivate != isPrivate)
        {
            IsPrivate = isPrivate;
            UpdateTimestamp();
        }
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }
}