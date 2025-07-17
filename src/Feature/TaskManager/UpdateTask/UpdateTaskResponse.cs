

using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Feature.TaskManager.UpdateTask;

public record class UpdateTaskResponse(Task task, string message);  

public record class Task(
    Guid Id,
    string Name,
    string Description,
    int Priority,
    PlanTaskStatus Status,
    DateTime? DueDate,
    DateTime? StartDate,
    long? TimeEstimate,
    long? TimeSpent,
    int OrderIndex,
    bool IsArchived,
    bool IsPrivate,
    Guid ListId,
    Guid CreatorId
);

