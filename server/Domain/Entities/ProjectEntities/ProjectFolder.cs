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
    public long NextItemOrder { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    private ProjectFolder() { }

    private ProjectFolder(Guid id, Guid projectSpaceId, string name, Customization customization, bool isPrivate, long orderKey, Guid creatorId, long nextItemOrder, DateTimeOffset? startDate, DateTimeOffset? dueDate)
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
        NextItemOrder = nextItemOrder;
        StartDate = startDate;
        DueDate = dueDate;
    }

    public static ProjectFolder Create(Guid projectSpaceId, string name, string color, string icon, bool isPrivate, Guid creatorId, long orderKey, DateTimeOffset? start = null, DateTimeOffset? due = null)
    {
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color required.", nameof(color));
        if (!IsValidColorCode(color)) throw new ArgumentException("Invalid color.", nameof(color));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));

        if (start.HasValue && due.HasValue && start > due) throw new ArgumentException("Start date cannot be later than due date.", nameof(start));
        var customization = Customization.Create(color.Trim(), icon.Trim());
        return new ProjectFolder(Guid.NewGuid(), projectSpaceId, name?.Trim() ?? throw new ArgumentNullException(nameof(name)), customization, isPrivate, orderKey, creatorId, 10_000_000L, start, due);
    }

    public long GetNextItemOrderAndIncrement()
    {
        var currentOrder = NextItemOrder;
        NextItemOrder += 10_000_000L;
        return currentOrder;
    }

    public void UpdateName(string name)
    {
        var candidateName = name.Trim();
        ValidateName(candidateName);
        if (candidateName == Name) return;
        Name = candidateName;
        UpdateTimestamp();
    }

    public void UpdateColor(string color)
    {
        var candidateColor = color.Trim();
        var newCustomization = Customization.Create(candidateColor, Customization.Icon);
        if (newCustomization.Equals(Customization)) return;
        Customization = newCustomization;
        UpdateTimestamp();
    }

    public void UpdateIcon(string icon)
    {
        var candidateIcon = icon.Trim();
        var newCustomization = Customization.Create(Customization.Color, candidateIcon);
        if (newCustomization.Equals(Customization)) return;
        Customization = newCustomization;
        UpdateTimestamp();
    }

    public void UpdatePrivate(bool isPrivate)
    {
        if (IsPrivate == isPrivate) return;
        IsPrivate = isPrivate;
        UpdateTimestamp();
    }

    public void UpdateDates(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));

        var changed = false;
        if (StartDate != startDate) { StartDate = startDate; changed = true; }
        if (DueDate != dueDate) { DueDate = dueDate; changed = true; }
        
        if (changed) UpdateTimestamp();
    }

    public void UpdateOrderKey(long orderKey)
    {
        if (orderKey < 0) throw new ArgumentOutOfRangeException(nameof(orderKey), "Order key cannot be negative.");
        if (OrderKey == orderKey) return;
        OrderKey = orderKey;
        UpdateTimestamp();
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("Name too long.", nameof(name));
    }
}
