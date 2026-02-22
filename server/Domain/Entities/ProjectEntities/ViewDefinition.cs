using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities.ProjectEntities;

public class ViewDefinition : Entity
{
    public EntityLayerType LayerType { get; private set; }
    public Guid LayerId { get; private set; }
    public string Name { get; private set; } = null!;
    public ViewType ViewType { get; private set; }
    public bool IsDefault { get; private set; }
    
    // Shared configurations stored as JSON for flexibility
    public string? FilterConfigJson { get; private set; }
    public string? DisplayConfigJson { get; private set; }

    private ViewDefinition() { }

    private ViewDefinition(
        Guid layerId,
        EntityLayerType layerType,
        string name,
        ViewType viewType,
        bool isDefault,
        Guid creatorId)
    {
        LayerId = layerId;
        LayerType = layerType;
        Name = name;
        ViewType = viewType;
        IsDefault = isDefault;
        CreatorId = creatorId;
    }

    public static ViewDefinition Create(
        Guid layerId,
        EntityLayerType layerType,
        string name,
        ViewType viewType,
        Guid creatorId,
        bool isDefault = false)
    {
        return new ViewDefinition(layerId, layerType, name, viewType, isDefault, creatorId);
    }

    public void Update(string? name, bool? isDefault)
    {
        if (name != null) Name = name;
        if (isDefault != null) IsDefault = isDefault.Value;
        UpdateTimestamp();
    }

    public void UpdateConfigs(string? filterConfigJson, string? displayConfigJson)
    {
        FilterConfigJson = filterConfigJson;
        DisplayConfigJson = displayConfigJson;
        UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdateTimestamp();
    }
}
