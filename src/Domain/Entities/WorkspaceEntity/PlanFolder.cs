using System;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Domain.Entities.WorkspaceEntity;

public class PlanFolder : Agregate<Guid>
{
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; }
    public ICollection<PlanList> Lists { get; set; } = new List<PlanList>();
    public ICollection<UserFolder> Members { get; set; } = new List<UserFolder>();


    public Guid CreatorId { get; private set; }
}
