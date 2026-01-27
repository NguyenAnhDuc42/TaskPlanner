using Domain.Enums;

namespace Application.Common;

public record HierarchyPath
{
    public Guid TargetId { get; init; }
    public EntityType TargetType { get; init; }
    public Guid WorkspaceId { get; init; }
    public bool TargetIsPrivate { get; init; }
    public Guid TargetCreatorId { get; init; }
    public bool TargetIsArchived { get; init; }
    
    /// <summary>
    /// Ancestors from immediate parent up to the Workspace-level branch (Space or ChatRoom).
    /// </summary>
    public List<EntityPathNode> Ancestors { get; init; } = new(); 
    
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Returns true if any node in the path (Target or Ancestors) is Private.
    /// </summary>
    public bool AnyPrivate => TargetIsPrivate || Ancestors.Any(a => a.IsPrivate);
}

public record EntityPathNode
{
    public Guid Id { get; init; }
    public EntityType Type { get; init; }
    public bool IsPrivate { get; init; }
}
