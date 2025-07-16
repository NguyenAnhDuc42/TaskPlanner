using System;
using src.Domain.Entities.WorkspaceEntity.Relationships;
namespace src.Domain.Entities.WorkspaceEntity;

public class PlanTask : Entity<Guid>
{
    public Guid WorkspaceId { get; set; } // Root level - for quick workspace queries
    public Guid SpaceId { get; set; } // Team level - for team reporting
    public Guid? FolderId { get; set; }
    public Guid ListId { get; set; } // Direct parent - task container

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public long? TimeEstimate { get; set; }
    public long? TimeSpent { get; set; }
    public int OrderIndex { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPrivate { get; set; }

    public ICollection<UserTask> Asignees { get; set; } = new List<UserTask>();
    public Guid CreatorId { get; set; }

    private PlanTask() { }
    private PlanTask(Guid id, string name, string description, int priority, DateTime? startDate, DateTime? dueDate, long? timeEstimate, long? timeSpent, int orderIndex, bool isArchived, bool isPrivate, Guid workspaceId, Guid spaceId, Guid? folderId, Guid listId, Guid creatorId) : base(id)
    {
        Name = name;
        WorkspaceId = workspaceId;
        SpaceId = spaceId;
        FolderId = folderId;
        ListId = listId;
        CreatorId = creatorId;
        Description = description;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        TimeEstimate = timeEstimate;
        TimeSpent = timeSpent;
        OrderIndex = orderIndex;
        IsArchived = isArchived;
        IsPrivate = isPrivate;
    }

    public static PlanTask Create(string name, string description, int priority, DateTime? startDate, DateTime? dueDate, bool isPrivate,  Guid workspaceId, Guid spaceId, Guid? folderId, Guid listId, Guid creatorId)
    {
        var task = new PlanTask(Guid.NewGuid(), name, description, priority, startDate, dueDate, null, null, 0, false, isPrivate, workspaceId, spaceId, folderId, listId, creatorId);
        return task;
    }

}
