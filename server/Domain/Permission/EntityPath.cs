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
    public bool? IsDirectParentPrivate { get; init; } // STOPS inheritance here

    // Flattened ancestry for fast access
    public required Guid ProjectWorkspaceId { get; init; }
    public Guid? ProjectSpaceId { get; init; }
    public Guid? ProjectFolderId { get; init; }
    public Guid? ProjectListId { get; init; }

    // Ancestor privacy info for waterfall resolution
    public bool? IsSpacePrivate { get; init; } // STOPS if true
    public bool? IsFolderPrivate { get; init; } // STOPS if true
}