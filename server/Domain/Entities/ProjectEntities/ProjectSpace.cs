using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;

namespace Domain.Entities.ProjectEntities;

public sealed class ProjectSpace : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public bool InheritStatus { get; private set; } = false;
    public long OrderKey { get; private set; }

    public long NextItemOrder { get; private set; }

    private ProjectSpace() { }

    private ProjectSpace(Guid id, Guid projectWorkspaceId, string name, string? description, Customization customization, bool isPrivate, bool inheritStatus, Guid creatorId, long orderKey, long nextItemOrder)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Customization = customization ?? Customization.CreateDefault();
        IsPrivate = isPrivate;
        InheritStatus = inheritStatus;
        CreatorId = creatorId;
        OrderKey = orderKey;
        IsArchived = false;
        NextItemOrder = nextItemOrder;
    }

    public static ProjectSpace Create(Guid projectWorkspaceId, string name, string? description, Customization? customization, bool isPrivate, bool inheritStatus, Guid creatorId, long orderKey)
    {
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new ProjectSpace(Guid.NewGuid(), projectWorkspaceId, name?.Trim() ?? throw new ArgumentNullException(nameof(name)),
            string.IsNullOrWhiteSpace(description) ? null : description?.Trim(), customization ?? Customization.CreateDefault(), isPrivate, inheritStatus, creatorId, orderKey, 10_000_000L);
    }

    public long GetNextItemOrderAndIncrement()
    {
        var currentOrder = NextItemOrder;
        NextItemOrder += 10_000_000L;
        return currentOrder;
    }

    public void UpdateName(string name)
    {
        var candidateName = name.Trim() == string.Empty
            ? throw new ArgumentException("Name cannot be empty.", nameof(name))
            : name.Trim();
        ValidateName(candidateName);

        if (candidateName == Name) return;

        Name = candidateName;
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        var candidateDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        ValidateDescription(candidateDescription);

        if (candidateDescription == Description) return;

        Description = candidateDescription;
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
        if (IsPrivate != isPrivate)
        {
            IsPrivate = isPrivate;
            UpdateTimestamp();
        }
    }

    public void UpdateOrderKey(long orderKey)
    {
        if (OrderKey == orderKey) return;
        OrderKey = orderKey;
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

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Space name cannot be empty.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("Space name cannot exceed 100 characters.", nameof(name));
    }

    private static void ValidateDescription(string? description)
    {
        if (description?.Length > 500) throw new ArgumentException("Space description cannot exceed 500 characters.", nameof(description));
    }
}
