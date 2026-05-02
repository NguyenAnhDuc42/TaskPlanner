using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class ProjectFolder : TenantEntity
{
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public Guid DefaultDocumentId { get; private set; }
    public string Color { get; private set; } = "#FFFFFF";
    public string? Icon { get; private set; }
    public string OrderKey { get; private set; } = null!;
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public Guid? WorkflowId { get; private set; }
    public Guid? StatusId { get; private set; }

    private ProjectFolder() { }
    private ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name, string slug, Guid defaultDocumentId, string orderKey, bool isPrivate, Guid creatorId, string color, string? icon, DateTimeOffset? startDate, DateTimeOffset? dueDate)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Slug = slug;
        DefaultDocumentId = defaultDocumentId;
        OrderKey = orderKey;
        IsPrivate = isPrivate;
        Color = color;
        Icon = icon;
        IsArchived = false;
        StartDate = startDate;
        DueDate = dueDate;

        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static ProjectFolder Create(Guid projectWorkspaceId, Guid projectSpaceId, string name, string slug, Guid defaultDocumentId, string orderKey, bool isPrivate, Guid creatorId, string? color = null, string? icon = null, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null)
    {
        var folder = new ProjectFolder(
            Guid.NewGuid(), 
            projectWorkspaceId, 
            projectSpaceId, 
            name,
            slug,
            defaultDocumentId, 
            orderKey, 
            isPrivate, 
            creatorId, 
            color ?? "#FFFFFF", 
            icon,
            startDate,
            dueDate);

        folder.WorkflowId = null;
        return folder;
    }

    public static ProjectFolder CreateDefault(Guid projectWorkspaceId, Guid projectSpaceId, Guid defaultDocumentId, Guid creatorId)
    {
        return Create(
            projectWorkspaceId,
            projectSpaceId,
            "Getting Started",
            "getting-started",
            defaultDocumentId,
            FractionalIndex.Start(),
            isPrivate: false,
            creatorId: creatorId
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

    public void UpdatePrivate(bool isPrivate)
    {
        EnsureNotArchived();
        if (IsPrivate == isPrivate) return;
        IsPrivate = isPrivate;
        UpdateTimestamp();
    }

    public void UpdateOrderKey(string orderKey)
    {
        EnsureNotArchived();
        if (OrderKey == orderKey) return;
        OrderKey = orderKey;
        UpdateTimestamp();
    }

    public void UpdateWorkflow(Guid? workflowId)
    {
        EnsureNotArchived();
        if (WorkflowId == workflowId) return;
        WorkflowId = workflowId;
        UpdateTimestamp();
    }

    public void UpdateStatus(Guid? statusId)
    {
        EnsureNotArchived();
        if (StatusId == statusId) return;
        StatusId = statusId;
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
        if (IsArchived) throw new BusinessRuleException("Cannot modify an archived folder.");
    }
}
