using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using static Domain.Common.ColorValidator;
using Domain.Common.Interfaces;
using Domain.Exceptions;
using Domain.Events.Folder;

namespace Domain.Entities;

public sealed class ProjectFolder : TenantEntity
{
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public string OrderKey { get; private set; } = FractionalIndex.Start();
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public Guid? WorkflowId { get; private set; }
    public Guid? StatusId { get; private set; }

    private ProjectFolder() { }

    private ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name, string slug, string? description, string orderKey, bool isPrivate, Guid creatorId, Customization customization, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        OrderKey = orderKey;
        IsPrivate = isPrivate;
        CreatorId = creatorId;
        Customization = customization;
        IsArchived = false;
        StartDate = startDate;
        DueDate = dueDate;
    }

    public static ProjectFolder Create(Guid projectWorkspaceId, Guid projectSpaceId, string name, string slug, string? description, string orderKey, bool isPrivate, Guid creatorId, Customization? customization = null, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null)
    {
        if (creatorId == Guid.Empty) throw new BusinessRuleException("CreatorId cannot be empty.");
        if (string.IsNullOrWhiteSpace(orderKey)) throw new BusinessRuleException("OrderKey cannot be empty.");
        
        ValidateBasicInfo(name, slug, description);
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate) 
            throw new BusinessRuleException("Start date cannot be later than due date.");
        
        var folder = new ProjectFolder(
            Guid.NewGuid(), 
            projectWorkspaceId,
            projectSpaceId, 
            name.Trim(),
            slug.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(), 
            orderKey, 
            isPrivate, 
            creatorId, 
            customization ?? Customization.CreateDefault(), 
            startDate, 
            dueDate);

        folder.WorkflowId = null; // Default to inheriting from Space
        folder.AddDomainEvent(new FolderCreatedEvent(projectWorkspaceId, projectSpaceId, folder.Id, creatorId));
        return folder;
    }

    public void Delete(Guid workspaceId, Guid userId)
    {
        SoftDelete();
        AddDomainEvent(new FolderDeletedEvent(workspaceId, ProjectSpaceId, Id, userId));
    }

    public void UpdateBasicInfo(string? name, string? slug, string? description)
    {
        EnsureNotArchived();

        var candidateName = name?.Trim() ?? Name;
        var candidateSlug = slug?.Trim().ToLowerInvariant() ?? Slug;
        var candidateDescription = description?.Trim() ?? Description;

        if (candidateName == Name && candidateSlug == Slug && candidateDescription == Description) return;

        ValidateBasicInfo(candidateName, candidateSlug, candidateDescription);

        Name = candidateName;
        Slug = candidateSlug;
        Description = string.IsNullOrWhiteSpace(candidateDescription) ? null : candidateDescription;

        UpdateTimestamp();
    }

    public void UpdateCustomization(string? color, string? icon)
    {
        EnsureNotArchived();
        if (color is null && icon is null) return;

        var newColor = color?.Trim() ?? Customization.Color;
        var newIcon = icon?.Trim() ?? Customization.Icon;
        var newCustomization = Customization.Create(newColor, newIcon);

        if (!newCustomization.Equals(Customization))
        {
            Customization = newCustomization;
            UpdateTimestamp();
        }
    }

    public void UpdateDates(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        EnsureNotArchived();
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new BusinessRuleException("Start date cannot be later than due date.");

        var changed = false;
        if (StartDate != startDate) { StartDate = startDate; changed = true; }
        if (DueDate != dueDate) { DueDate = dueDate; changed = true; }
        
        if (changed) UpdateTimestamp();
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
        if (string.IsNullOrWhiteSpace(orderKey)) throw new BusinessRuleException("OrderKey cannot be empty.");
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

    private static void ValidateBasicInfo(string name, string slug, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Name cannot be empty.");
        if (name.Length > 100) throw new BusinessRuleException("Name too long.");
        if (string.IsNullOrWhiteSpace(slug)) throw new BusinessRuleException("Slug cannot be empty.");
        if (description?.Length > 1000) throw new BusinessRuleException("Description too long.");
    }
}
