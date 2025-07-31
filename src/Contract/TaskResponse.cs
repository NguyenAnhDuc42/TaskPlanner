using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Contract;


public record class TaskDetail(
    Guid Id,
    string Name,
    string Description,
    int Priority,
    Task Status,
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

public record class TaskSummary(Guid Id, string Name, DateTime? DueDate, int Priority, List<UserSummary> assignees);

