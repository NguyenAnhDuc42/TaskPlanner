using System;
using src.Domain.Entities.UserEntity;
using src.Domain.Entities.WorkspaceEntity.Relationships;


namespace src.Domain.Entities.WorkspaceEntity;

public class Space : Agregate<Guid>
{
    public Guid WorkspaceId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;

    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; }
    public ICollection<PlanList> Lists { get; set; } = new List<PlanList>();
    public ICollection<PlanFolder> Folders { get; set; } = new List<PlanFolder>();
    public ICollection<UserSpace> Members { get; set; } = new List<UserSpace>();


    public Guid CreatorId { get; private set; }

    private Space() { }
    private Space(Guid id, string name, string icon, Guid workspaceId, Guid creatorId) : base(id)
    {
        Name = name;
        Icon = icon;
        WorkspaceId = workspaceId;
        CreatorId = creatorId;
    }

    public static Space Create(string name, string icon, Guid workspaceId,Guid creatorId)
    {
       var space = new Space(Guid.NewGuid(), name, icon, workspaceId,creatorId);
       var list = PlanList.Create("List",workspaceId,space.Id,null,creatorId);
        space.Lists.Add(list);
       return space;
    }


}
