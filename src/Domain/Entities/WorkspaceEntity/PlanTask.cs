using System;
using src.Domain.Entities.WorkspaceEntity.Relationships;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
namespace src.Domain.Entities.WorkspaceEntity;

public class PlanTask : Entity<Guid>
{
    public Guid WorkspaceId { get; private set; } // Root level - for quick workspace queries
    public Guid SpaceId { get; private set; } // Team level - for team reporting
    public Guid? FolderId { get; private set; }
    public Guid ListId { get; private set; } // Direct parent - task container

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public PlanTaskStatus Status { get; private set; }
    public Guid MyProperty { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? StartDate { get; private set; }
    public long? TimeEstimate { get; private set; }
    public long? TimeSpent { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsPrivate { get; private set; }

    public ICollection<UserTask> Asignees { get; set; } = new List<UserTask>();
    public Guid CreatorId { get; private set; }

    private PlanTask() { }
    private PlanTask(Guid id, string name, string description, int priority, PlanTaskStatus status, DateTime? startDate, DateTime? dueDate, long? timeEstimate, long? timeSpent, int orderIndex, bool isArchived, bool isPrivate, Guid workspaceId, Guid spaceId, Guid? folderId, Guid listId, Guid creatorId) : base(id)
    {
        Name = name;
        WorkspaceId = workspaceId;
        SpaceId = spaceId;
        FolderId = folderId;
        ListId = listId;
        CreatorId = creatorId;
        Description = description;
        Priority = priority;
        Status = status;
        StartDate = startDate;
        DueDate = dueDate;
        TimeEstimate = timeEstimate;
        TimeSpent = timeSpent;
        OrderIndex = orderIndex;
        IsArchived = isArchived;
        IsPrivate = isPrivate;
    }

    public static PlanTask Create(string name, string description, int priority,PlanTaskStatus status, DateTime? startDate, DateTime? dueDate, bool isPrivate, Guid workspaceId, Guid spaceId, Guid? folderId, Guid listId, Guid creatorId)
    {
        var task = new PlanTask(Guid.NewGuid(), name, description, priority, status, startDate, dueDate, null, null, 0, false, isPrivate, workspaceId, spaceId, folderId, listId, creatorId);
        return task;
    }

    public void Update(string? name, string? description, int priority, PlanTaskStatus status, DateTime? startDate, DateTime? dueDate, bool isPrivate)
    {
        Name = name;
        Description = description;
        Priority = priority;
        Status = status;
        StartDate = startDate;
        DueDate = dueDate;
        IsPrivate = isPrivate;

    }

}
