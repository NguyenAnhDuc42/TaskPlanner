namespace src.Feature.TaskManager.GetInfoTask;

public record class Task(
    Guid Id,
    string Name,
    string Description,
    int Priority,
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
