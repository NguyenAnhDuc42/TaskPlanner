using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Exceptions;

namespace Domain.Entities.ProjectEntities;

public sealed class ProjectSpace : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public string OrderKey { get; private set; } = FractionalIndex.Start();

    private ProjectSpace() { }

    private ProjectSpace(Guid id, Guid projectWorkspaceId, string name, string slug, string? description, Customization customization, bool isPrivate, Guid creatorId, string orderKey)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Customization = customization ?? Customization.CreateDefault();
        IsPrivate = isPrivate;
        CreatorId = creatorId;
        OrderKey = orderKey;
        IsArchived = false;
    }

    public static ProjectSpace Create(Guid projectWorkspaceId, string name, string slug, string? description, Customization? customization, bool isPrivate, Guid creatorId, string orderKey)
    {
        if (creatorId == Guid.Empty) throw new BusinessRuleException("CreatorId cannot be empty.");
        if (string.IsNullOrWhiteSpace(orderKey)) throw new BusinessRuleException("OrderKey cannot be empty.");
        
        ValidateBasicInfo(name, slug, description);

        return new ProjectSpace(
            Guid.NewGuid(), 
            projectWorkspaceId, 
            name.Trim(),
            slug.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(), 
            customization ?? Customization.CreateDefault(), 
            isPrivate, 
            creatorId, 
            orderKey);
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
        if (string.IsNullOrWhiteSpace(orderKey)) throw new BusinessRuleException("OrderKey cannot be empty.");
        if (OrderKey == orderKey) return;
        OrderKey = orderKey;
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

    private static void ValidateBasicInfo(string name, string slug, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Space name cannot be empty.");
        if (name.Length > 100) throw new BusinessRuleException("Space name cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(slug)) throw new BusinessRuleException("Slug cannot be empty.");
        if (description?.Length > 500) throw new BusinessRuleException("Space description cannot exceed 500 characters.");
    }
}
