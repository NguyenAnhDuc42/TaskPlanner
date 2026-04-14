using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class EntityAssetLink : TenantEntity
{
    public Guid AssetId { get; private set; }
    public AssetType AssetType { get; private set; }
    
    public Guid ParentEntityId { get; private set; }
    public EntityType ParentEntityType { get; private set; }

    private EntityAssetLink() { }

    private EntityAssetLink(Guid projectWorkspaceId, Guid assetId, AssetType assetType, Guid parentEntityId, EntityType parentEntityType, Guid creatorId)
        : base(Guid.NewGuid(), projectWorkspaceId)
    {
        AssetId = assetId;
        AssetType = assetType;
        ParentEntityId = parentEntityId;
        ParentEntityType = parentEntityType;
        CreatorId = creatorId;
    }

    public static EntityAssetLink Create(Guid projectWorkspaceId, Guid assetId, AssetType assetType, Guid parentEntityId, EntityType parentEntityType, Guid creatorId)
    {
        return new EntityAssetLink(projectWorkspaceId, assetId, assetType, parentEntityId, parentEntityType, creatorId);
    }
}
