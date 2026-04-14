using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities;

public class ViewDefinition : TenantEntity
{
    public EntityLayerType LayerType { get; private set; }
    public Guid LayerId { get; private set; }
    public string Name { get; private set; } = null!;
    public ViewType ViewType { get; private set; }
    public bool IsDefault { get; private set; }
    
    // Shared configurations stored as JSON in DB but typed in Domain
    public ViewFilterConfig FilterConfig { get; private set; } = ViewFilterConfig.CreateDefault();
    public string? DisplayConfigJson { get; private set; } // TODO: Make this type-safe later

    private ViewDefinition() { }

    private ViewDefinition(
        Guid projectWorkspaceId,
        Guid layerId,
        EntityLayerType layerType,
        string name,
        ViewType viewType,
        bool isDefault,
        Guid creatorId)
        : base(Guid.NewGuid(), projectWorkspaceId)
    {
        LayerId = layerId;
        LayerType = layerType;
        Name = name;
        ViewType = viewType;
        IsDefault = isDefault;
        CreatorId = creatorId;
    }

    public static ViewDefinition Create(
        Guid projectWorkspaceId,
        Guid layerId,
        EntityLayerType layerType,
        string name,
        ViewType viewType,
        Guid creatorId,
        bool isDefault = false)
    {
        return new ViewDefinition(projectWorkspaceId, layerId, layerType, name, viewType, isDefault, creatorId);
    }

    public void Update(string? name, bool? isDefault)
    {
        if (name != null) Name = name;
        if (isDefault != null) IsDefault = isDefault.Value;
        UpdateTimestamp();
    }

    public void UpdateConfigs(ViewFilterConfig? filterConfig, string? displayConfigJson)
    {
        if (filterConfig != null) FilterConfig = filterConfig;
        if (displayConfigJson != null) DisplayConfigJson = displayConfigJson;
        UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdateTimestamp();
    }
}
