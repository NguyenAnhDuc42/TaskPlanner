using System;
using Domain.Common;

namespace Domain.Entities.Relationship;

public class TaskAssignment : Composite
{
    public Guid TaskId { get; private set; }
    public Guid AssigneeId { get; private set; }

    private TaskAssignment() { } // EF
    private TaskAssignment(Guid taskId, Guid assigneeId, Guid creatorId)
    {
        TaskId = taskId;
        AssigneeId = assigneeId;
        CreatorId = creatorId;
    }
    public static TaskAssignment Assign(Guid taskId, Guid assigneeId, Guid creatorId) =>
        new TaskAssignment(taskId, assigneeId, creatorId);

}
