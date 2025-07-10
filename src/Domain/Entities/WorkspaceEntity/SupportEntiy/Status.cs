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
    public HierarchyLevel Level { get; private set; }


    public static Status CreateForSpace(Guid spaceId, string name, string color, StatusType type, int? order = null)
    {
        return new Status
        {
            SpaceId = spaceId,
            Name = name,
            Color = color,
            Order = order ?? 0,
            Type = type,
            Level = HierarchyLevel.Space
        };
    }
     public static Status CreateForList(Guid spaceId, string name, string color, StatusType type, int? order = null)
    {
        return new Status
        {
            SpaceId = spaceId,
            Name = name,
            Color = color,
            Order = order ?? 0,
            Type = type,
            Level = HierarchyLevel.List
        };
    }

}

public enum StatusType
{
    Open = 0,
    InProgress = 1,
    Closed = 2
}

public enum HierarchyLevel
{
    Space = 0,
    Folder = 1,
    List = 2
}


