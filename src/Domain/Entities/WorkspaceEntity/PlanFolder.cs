using System;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Domain.Entities.WorkspaceEntity;

public class PlanFolder : Aggregate<Guid>
{
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; }
    public ICollection<PlanList> Lists { get; set; } = new List<PlanList>();
    public ICollection<UserFolder> Members { get; set; } = new List<UserFolder>();

    public Guid CreatorId { get; private set; }
    private PlanFolder() { }

    private PlanFolder(Guid id, string name, Guid workspaceId, Guid spaceId, Guid creatorId) : base(id)
    {
        Name = name;
        WorkspaceId = workspaceId;
        SpaceId = spaceId;
        CreatorId = creatorId;
    }

    public static PlanFolder Create(string name, Guid workspaceId, Guid spaceId, Guid creatorId)
    {
        var folder = new PlanFolder(Guid.NewGuid(), name, workspaceId, spaceId, creatorId);
        var list = PlanList.Create("List",workspaceId,spaceId,folder.Id,creatorId);
        folder.Lists.Add(list);
        return folder;
    }

}
