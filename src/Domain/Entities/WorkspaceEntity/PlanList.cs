
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Domain.Entities.WorkspaceEntity;

public class PlanList : Aggregate<Guid>
{
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid? FolderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; }
    public int OrderIndex { get; private set; }
    public ICollection<PlanTask> Tasks { get; set; } = new List<PlanTask>();
    public ICollection<UserList> Members { get; set; } = new List<UserList>();


    public Guid CreatorId { get; private set; }
    private PlanList() { }
    private PlanList(Guid id, string name, Guid workspaceId, Guid spaceId, Guid? folderId, Guid creatorId) : base(id)
    {
        Name = name;
        WorkspaceId = workspaceId;
        SpaceId = spaceId;
        FolderId = folderId;
        CreatorId = creatorId;
    }

    public static PlanList Create(string name, Guid workspaceId,Guid spaceId, Guid? folderId,Guid creatorId)
    {
        var list = new PlanList(Guid.NewGuid(), name, workspaceId,spaceId, folderId,creatorId);
        return list;
    }
}
