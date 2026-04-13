using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class EntityAssetLink : Entity
{
    public Guid AssetId { get; private set; }
    public AssetType AssetType { get; private set; }
    
    public Guid ParentEntityId { get; private set; }
    public EntityType ParentEntityType { get; private set; }

    private EntityAssetLink() { }

    private EntityAssetLink(Guid assetId, AssetType assetType, Guid parentEntityId, EntityType parentEntityType, Guid creatorId)
    {
        AssetId = assetId;
        AssetType = assetType;
        ParentEntityId = parentEntityId;
        ParentEntityType = parentEntityType;
        CreatorId = creatorId;
    }

    public static EntityAssetLink Create(Guid assetId, AssetType assetType, Guid parentEntityId, EntityType parentEntityType, Guid creatorId)
    {
        return new EntityAssetLink(assetId, assetType, parentEntityId, parentEntityType, creatorId);
    }
}
