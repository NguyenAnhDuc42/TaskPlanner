using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;

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

    public void UpdateName(string name)
    {
        var candidateName = name.Trim() == string.Empty
            ? throw new ArgumentException("Name cannot be empty.", nameof(name))
            : name.Trim();
        ValidateBasicInfo(candidateName);
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

    public void UpdateStartDate(DateTimeOffset? startDate)
    {
        if (startDate.HasValue && DueDate.HasValue && startDate > DueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));
        if (StartDate == startDate) return;
        StartDate = startDate;
        UpdateTimestamp();
    }

    public void UpdateDueDate(DateTimeOffset? dueDate)
    {
        if (StartDate.HasValue && dueDate.HasValue && StartDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(dueDate));
        if (DueDate == dueDate) return;
        DueDate = dueDate;
        UpdateTimestamp();
    }

    public void UpdatePrivate(bool isPrivate)
    {
        if (IsPrivate == isPrivate) return;
        IsPrivate = isPrivate;
        UpdateTimestamp();
    }

    public void UpdateOrderKey(long orderKey)
    {
        if (OrderKey == orderKey) return;
        OrderKey = orderKey;
        UpdateTimestamp();
    }

    public void UpdateProjectFolderId(Guid? projectFolderId)
    {
        if (ProjectFolderId == projectFolderId) return;
        ProjectFolderId = projectFolderId;
        UpdateTimestamp();
    }

    public void UpdateInheritStatus(bool inheritStatus)
    {
        if (InheritStatus == inheritStatus) return;
        InheritStatus = inheritStatus;
        UpdateTimestamp();
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }

    private static void ValidateBasicInfo(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("List name cannot be empty.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("List name cannot exceed 100 characters.", nameof(name));
    }
}
