using System;
using Domain.Common;

namespace Domain.Entities.Relationship;

public class TaskAssignment : Composite
{
    public Guid TaskId { get; private set; }
    public Guid AssigneeId { get; private set; }
    public Guid AssignedById { get; private set; }

    private TaskAssignment() { } // EF
    private TaskAssignment(Guid taskId, Guid assigneeId, Guid assignedById)
    {
        TaskId = taskId;
        AssigneeId = assigneeId;
        AssignedById = assignedById;
    }
    public static TaskAssignment Assign(Guid taskId, Guid assigneeId, Guid assignedById) =>
        new TaskAssignment(taskId, assigneeId, assignedById);

}
