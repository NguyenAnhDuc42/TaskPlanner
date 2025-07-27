using System;

namespace src.Domain.Entities.WorkspaceEntity.SupportEntiy;

public class Status : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public StatusType Type { get; private set; } // Open, InProgress, Closed

    public Guid SpaceId { get; set; }
    public static Status Create(string name, string color, StatusType? type, Guid spaceId)
    {
        return new Status
        {
            Name = name,
            Color = color,
            Type = type ?? StatusType.NotStarted,
            SpaceId = spaceId
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



public enum PlanTaskStatus //temp
{
    ToDo = 0,
    InProgress = 1 ,
    InReview = 2,
    Done = 3
}


