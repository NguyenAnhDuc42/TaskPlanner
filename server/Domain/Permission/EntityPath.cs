using Domain.Enums.RelationShip;

namespace Domain.Permission;

public class EntityPath
{
    public required Guid EntityId { get; init; }
    public required EntityLayerType EntityLayer { get; init; }
    public required bool IsPrivate { get; init; }

    // Direct parent - tells us the actual container
    public Guid? DirectParentId { get; init; }
    public EntityLayerType? DirectParentType { get; init; }

    // Flattened ancestry for fast access
    public required Guid ProjectWorkspaceId { get; init; }
    public Guid? ProjectSpaceId { get; init; }
    public Guid? ProjectFolderId { get; init; }
    public Guid? ProjectListId { get; init; }
}   