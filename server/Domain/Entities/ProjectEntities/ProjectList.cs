using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Entities.Relationship;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.ProjectEntities;

public sealed class ProjectList : Entity
{
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public long OrderKey { get; private set; }
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public bool InheritStatus { get; private set; } = false;
    public long NextItemOrder { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
 
    private ProjectList() { }

    private ProjectList(Guid id, Guid projectSpaceId, Guid? projectFolderId, string name, Customization customization, bool isPrivate, bool inheritStatus, long orderKey, Guid creatorId, DateTimeOffset? startDate, DateTimeOffset? dueDate, long nextItemOrder)
    {
        Id = id;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Customization = customization ?? Customization.CreateDefault();
        IsPrivate = isPrivate;
        InheritStatus = inheritStatus;
        OrderKey = orderKey;
        NextItemOrder = nextItemOrder;
        CreatorId = creatorId;
        StartDate = startDate;
        DueDate = dueDate;
        IsArchived = false;
    }

    public static ProjectList Create(Guid projectSpaceId, Guid? projectFolderId, string name, Customization? customization, bool isPrivate, bool inheritStatus, Guid creatorId, long orderKey, DateTimeOffset? start = null, DateTimeOffset? due = null)
    {
        var candidateName = name?.Trim() ?? throw new ArgumentNullException(nameof(name));
        if (string.IsNullOrWhiteSpace(candidateName)) throw new ArgumentException("List name cannot be empty.", nameof(name));
        if (candidateName.Length > 100) throw new ArgumentException("List name cannot exceed 100 characters.", nameof(name));
        if (start.HasValue && due.HasValue && start > due) throw new ArgumentException("Start date cannot be later than due date.", nameof(start));
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));

        return new ProjectList(Guid.NewGuid(), projectSpaceId, projectFolderId, candidateName, customization ?? Customization.CreateDefault(), isPrivate, inheritStatus, orderKey, creatorId, start, due,10_000_000L);
    }

    public long GetNextItemOrderAndIncrement()
    {
        var currentOrder = this.NextItemOrder;
        this.NextItemOrder += 10_000_000L;
        return currentOrder;
    }

    public void Update(string? name = null, string? color = null, string? icon = null, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, bool? isPrivate = null, long? orderKey = null, bool? isArchived = null, Guid? projectFolderId = null)
    {
        var changed = false;

        var candidateName = name is null ? Name : (name.Trim() == string.Empty ? throw new ArgumentException("Name cannot be empty.", nameof(name)) : name.Trim());

        ValidateBasicInfo(candidateName);

        if (candidateName != Name) { Name = candidateName; changed = true; }

        if (startDate.HasValue || dueDate.HasValue)
        {
            var finalStart = startDate ?? StartDate;
            var finalDue = dueDate ?? DueDate;
            if (finalStart.HasValue && finalDue.HasValue && finalStart > finalDue) throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));
            if (finalStart != StartDate || finalDue != DueDate) { StartDate = finalStart; DueDate = finalDue; changed = true; }
        }

        if (color is not null || icon is not null)
        {
            var c = color?.Trim() ?? Customization.Color;
            var i = icon?.Trim() ?? Customization.Icon;
            var newCustomization = Customization.Create(c, i);
            if (!newCustomization.Equals(Customization)) { Customization = newCustomization; changed = true; }
        }

        if (isPrivate.HasValue && isPrivate.Value != IsPrivate) { IsPrivate = isPrivate.Value; changed = true; }
        if (orderKey.HasValue && orderKey != OrderKey) { OrderKey = orderKey.Value; changed = true; }
        if (projectFolderId is not null && projectFolderId != ProjectFolderId) { ProjectFolderId = projectFolderId; changed = true; }
        if (isArchived.HasValue && isArchived.Value != IsArchived) { IsArchived = isArchived.Value; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public void UpdateDetails(string name, string? color = null, string? icon = null)
    {
        var changed = false;
        var candidateName = name.Trim() == string.Empty ? throw new ArgumentException("Name cannot be empty.", nameof(name)) : name.Trim();
        ValidateBasicInfo(candidateName);
        if (candidateName != Name)
        {
            Name = candidateName;
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

    public void UpdateDates(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate) throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));
        
        var changed = false;
        if (StartDate != startDate) { StartDate = startDate; changed = true; }
        if (DueDate != dueDate) { DueDate = dueDate; changed = true; }
        
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

    private static void ValidateBasicInfo(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("List name cannot be empty.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("List name cannot exceed 100 characters.", nameof(name));
    }
}