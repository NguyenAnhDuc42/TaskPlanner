using System;

namespace src.Domain.Entities.WorkspaceEntity.SupportEntiy;

public class Status : Entity<Guid>
{
    public Guid? SpaceId { get; private set; }
    public Guid? ListId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public StatusType Type { get; private set; } // Open, InProgress, Closed
     public static Status CreateForList(Guid spaceId, string name, string color, StatusType type, int? order = null)
    {
        return new Status
        {
            SpaceId = spaceId,
            Name = name,
            Color = color,
            Order = order ?? 0,
            Type = type,
        };
    }

}

public enum StatusType
{
    NotStarted = 0,
    Active = 1,
    Done = 2,
    Closed = 3
}

public enum HierarchyLevel
{
    Space = 0,
    Folder = 1,
    List = 2
}

public enum PlanTaskStatus //temp
{
    ToDo = 0,
    InProgress = 1 ,
    InReview = 2,
    Done = 3
}


