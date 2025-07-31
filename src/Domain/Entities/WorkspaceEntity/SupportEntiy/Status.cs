using System;

namespace src.Domain.Entities.WorkspaceEntity.SupportEntiy;

public class Status : Entity<Guid>
{
    public Guid SpaceId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public StatusType Type { get; private set; } // Open, InProgress, Closed

    public ICollection<PlanTask> Tasks { get; set; } = new List<PlanTask>();
    private Status() { }
    private Status(Guid id, string name, string color, StatusType type, Guid spaceId) : base(id)
    {
        Name = name;
        Color = color;
        Type = type;
        SpaceId = spaceId;
    }
    public static Status Create(string name, string? color, StatusType? type, Guid spaceId)
    {
        return new Status(Guid.NewGuid(), name, color ?? string.Empty, type ?? StatusType.NotStarted, spaceId);
    }

}

public enum StatusType
{
    NotStarted = 0,
    Active = 1,
    Done = 2,
    Closed = 3
}



