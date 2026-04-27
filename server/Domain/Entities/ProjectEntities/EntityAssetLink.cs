using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class EntityAssetLink : TenantEntity
{
    public Guid AssetId { get; private set; }
    public AssetType AssetType { get; private set; }
    
    public Guid? ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public Guid? ProjectTaskId { get; private set; }
    public Guid? CommentId { get; private set; }

    private EntityAssetLink() { }

    private EntityAssetLink(Guid projectWorkspaceId, Guid assetId, AssetType assetType, Guid? projectSpaceId, Guid? projectFolderId, Guid? projectTaskId, Guid? commentId, Guid creatorId)
        : base(Guid.NewGuid(), projectWorkspaceId)
    {
        AssetId = assetId;
        AssetType = assetType;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        ProjectTaskId = projectTaskId;
        CommentId = commentId;
        InitializeAudit(creatorId);
    }

    public static EntityAssetLink Create(Guid projectWorkspaceId, Guid assetId, AssetType assetType, Guid? projectSpaceId, Guid? projectFolderId, Guid? projectTaskId, Guid? commentId, Guid creatorId)
    {
        return new EntityAssetLink(projectWorkspaceId, assetId, assetType, projectSpaceId, projectFolderId, projectTaskId, commentId, creatorId);
    }
}
