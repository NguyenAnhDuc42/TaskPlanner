using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class ProjectSpace : TenantEntity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Color { get; private set; } = "#FFFFFF";
    public string? Icon { get; private set; }
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public string OrderKey { get; private set; } = null!;
    public bool IsInheritingWorkflow { get; private set; } = true;
    public Guid? StatusId { get; private set; }
    public Guid DefaultDocumentId { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    private ProjectSpace() { }

    private ProjectSpace(Guid id, Guid projectWorkspaceId, string name, string slug, Guid defaultDocumentId, string color, string? icon, bool isPrivate, Guid creatorId, string orderKey)
        : base(id, projectWorkspaceId)
    {
        Name = name;
        Slug = slug;
        DefaultDocumentId = defaultDocumentId;
        Color = color;
        Icon = icon;
        IsPrivate = isPrivate;
        OrderKey = orderKey;
        IsArchived = false;
        IsInheritingWorkflow = true;

        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static ProjectSpace Create(Guid projectWorkspaceId, string name, string slug, Guid defaultDocumentId, string? color, string? icon, bool isPrivate, Guid creatorId, string orderKey)
    {
        var space = new ProjectSpace(
            Guid.NewGuid(), 
            projectWorkspaceId, 
            name,
            slug,
            defaultDocumentId, 
            color ?? "#FFFFFF", 
            icon,
            isPrivate, 
            creatorId, 
            orderKey);

        return space;
    }

    public static ProjectSpace CreateDefault(Guid projectWorkspaceId, Guid defaultDocumentId, Guid creatorId)
    {
        return Create(
            projectWorkspaceId,
            "Welcome Space",
            "welcome-space",
            defaultDocumentId,
            null,
            null,
            isPrivate: false,
            creatorId: creatorId,
            orderKey: FractionalIndex.Start()
        );
    }

    public void Delete()
    {
        SoftDelete();
    }

    public void UpdateName(string name)
    {
        EnsureNotArchived();
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateSlug(string slug)
    {
        EnsureNotArchived();
        if (Slug == slug) return;
        Slug = slug;
        UpdateTimestamp();
    }

    public void UpdateColor(string color)
    {
        EnsureNotArchived();
        if (Color == color) return;
        Color = color;
        UpdateTimestamp();
    }

    public void UpdateIcon(string? icon)
    {
        EnsureNotArchived();
        if (Icon == icon) return;
        Icon = icon;
        UpdateTimestamp();
    }

    public void UpdatePrivate(bool isPrivate)
    {
        EnsureNotArchived();
        if (IsPrivate != isPrivate)
        {
            IsPrivate = isPrivate;
            UpdateTimestamp();
        }
    }

    public void UpdateOrderKey(string orderKey)
    {
        EnsureNotArchived();
        if (OrderKey == orderKey) return;
        OrderKey = orderKey;
        UpdateTimestamp();
    }

    public void UpdateInheritWorkflow(bool isInherit)
    {
        EnsureNotArchived();
        if (IsInheritingWorkflow == isInherit) return;
        IsInheritingWorkflow = isInherit;
        UpdateTimestamp();
    }

    public void UpdateStatus(Guid? statusId)
    {
        EnsureNotArchived();
        if (StatusId == statusId) return;
        StatusId = statusId;
        UpdateTimestamp();
    }

    public void UpdateStartDate(DateTimeOffset? startDate)
    {
        EnsureNotArchived();
        if (StartDate == startDate) return;
        StartDate = startDate;
        UpdateTimestamp();
    }

    public void UpdateDueDate(DateTimeOffset? dueDate)
    {
        EnsureNotArchived();
        if (DueDate == dueDate) return;
        DueDate = dueDate;
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

    private void EnsureNotArchived()
    {
        if (IsArchived) throw new BusinessRuleException("Cannot modify an archived space.");
    }
}
