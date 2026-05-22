using Domain.Common;
using Domain.Exceptions;
using Domain.Enums;

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
    public Guid? StatusId { get; private set; }
    public Priority? Priority { get; private set; }

    private ProjectFolder() { }
    private ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name, string slug, Guid defaultDocumentId, string orderKey, bool isPrivate, Guid creatorId, string color, string? icon, DateTimeOffset? startDate, DateTimeOffset? dueDate, Priority? priority = null)
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
        Priority = priority;

        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static ProjectFolder Create(Guid projectWorkspaceId, Guid projectSpaceId, string name, string slug, Guid defaultDocumentId, string orderKey, bool isPrivate, Guid creatorId, string? color = null, string? icon = null, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, Priority? priority = null)
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
            dueDate,
            priority);

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

    public void Update(
        string? name = null,
        string? slug = null,
        string? color = null,
        string? icon = null,
        bool? isPrivate = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? dueDate = null,
        Guid? statusId = null,
        Priority? priority = null,
        string? orderKey = null)
    {
        EnsureNotArchived();
        bool updated = false;

        if (name != null && Name != name) { Name = name; updated = true; }
        if (slug != null && Slug != slug) { Slug = slug; updated = true; }
        if (color != null && Color != color) { Color = color; updated = true; }
        if (icon != null && Icon != icon) { Icon = icon; updated = true; }
        if (isPrivate != null && IsPrivate != isPrivate) { IsPrivate = isPrivate.Value; updated = true; }
        if (startDate != null && StartDate != startDate) { StartDate = startDate; updated = true; }
        if (dueDate != null && DueDate != dueDate) { DueDate = dueDate; updated = true; }
        if (statusId != null && StatusId != statusId) { StatusId = statusId; updated = true; }
        if (priority != null && Priority != priority) { Priority = priority; updated = true; }
        if (orderKey != null && OrderKey != orderKey) { OrderKey = orderKey; updated = true; }

        if (updated) UpdateTimestamp();
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
