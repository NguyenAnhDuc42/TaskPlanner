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
    public long OrderKey { get; private set; }
    public Guid CreatorId { get; private set; }
    public long NextEntityOrder { get; private set; } = 10_000_000L;

    private ProjectSpace() { }

    private ProjectSpace(Guid id, Guid projectWorkspaceId, string name, string? description, Customization customization, bool isPrivate, long orderKey, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Customization = customization ?? Customization.CreateDefault();
        IsPrivate = isPrivate;
        OrderKey = orderKey;
        CreatorId = creatorId;
        IsArchived = false;
    }

    public static ProjectSpace Create(Guid projectWorkspaceId, string name, string? description, Customization? customization, bool isPrivate, Guid creatorId, long orderKey)
    {
        if (creatorId == Guid.Empty) throw new ArgumentException("CreatorId cannot be empty.", nameof(creatorId));
        return new ProjectSpace(Guid.NewGuid(), projectWorkspaceId, name?.Trim() ?? throw new ArgumentNullException(nameof(name)), string.IsNullOrWhiteSpace(description) ? null : description?.Trim(), customization ?? Customization.CreateDefault(), isPrivate, orderKey, creatorId);
    }

    public long GetNextEntityOrderAndIncrement()
    {
        var currentOrder = NextEntityOrder;
        NextEntityOrder += 10_000_000L;
        return currentOrder;
    }
    public void Update(string? name = null, string? description = null, string? color = null, string? icon = null, bool? isPrivate = null, long? orderKey = null, bool? isArchived = null)
    {
        var changed = false;

        var candidateName = name is null ? Name : (name.Trim() == string.Empty ? throw new ArgumentException("Name cannot be empty.", nameof(name)) : name.Trim());
        var candidateDescription = description is null ? Description : (string.IsNullOrWhiteSpace(description.Trim()) ? null : description.Trim());

        ValidateBasicInfo(candidateName, candidateDescription);

        if (candidateName != Name) { Name = candidateName; changed = true; }
        if (candidateDescription != Description) { Description = candidateDescription; changed = true; }

        if (color is not null || icon is not null)
        {
            var c = color?.Trim() ?? Customization.Color;
            var i = icon?.Trim() ?? Customization.Icon;
            var newCustomization = Customization.Create(c, i);
            if (!newCustomization.Equals(Customization)) { Customization = newCustomization; changed = true; }
        }

        if (isPrivate.HasValue && isPrivate.Value != IsPrivate) { IsPrivate = isPrivate.Value; changed = true; }
        if (orderKey.HasValue && orderKey != OrderKey) { OrderKey = orderKey.Value; changed = true; }
        if (isArchived.HasValue && isArchived.Value != IsArchived) { IsArchived = isArchived.Value; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Space name cannot be empty.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("Space name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500) throw new ArgumentException("Space description cannot exceed 500 characters.", nameof(description));
    }
}